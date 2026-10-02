using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Makes double-booking a vet impossible at the database, not merely unlikely in the code.
    /// <para>
    /// The booking use cases re-check the slot inside their transaction, which is necessary but not
    /// sufficient: under PostgreSQL's default READ COMMITTED isolation, two concurrent bookings both
    /// read a free calendar, neither sees the other's uncommitted row, and both commit. Two owners
    /// are then told they have 11:00 with the same vet. Application-level checking cannot close
    /// that window — only the database can, because only the database sees both writes.
    /// </para>
    /// <para>
    /// An exclusion constraint refuses the second write outright. It also holds against every other
    /// path into the table: a future endpoint, a bulk import, a hand-run <c>UPDATE</c> during an
    /// incident. <c>tstzrange(starts_at, ends_at)</c> defaults to half-open <c>[)</c> bounds, which
    /// is the same overlap rule the slot calculator uses, so back-to-back appointments stay legal.
    /// </para>
    /// </summary>
    public partial class AppointmentSlotExclusion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GiST cannot index plain equality on text on its own; btree_gist adds the operator
            // classes that let branch_id and staff_id take part in the same exclusion constraint as
            // the time range.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            // Scoped to appointments that actually hold their time. A cancelled or completed visit
            // must not block the slot it released — the same set as
            // AppointmentTransitions.OccupyingStatuses, which is stored as the enum member name.
            migrationBuilder.Sql(
                """
                ALTER TABLE appointments
                ADD CONSTRAINT ex_appointments_staff_slot
                EXCLUDE USING gist (
                    branch_id WITH =,
                    staff_id WITH =,
                    tstzrange(starts_at, ends_at) WITH &&
                )
                WHERE (
                    staff_id IS NOT NULL
                    AND status IN ('Booked', 'Confirmed', 'CheckedIn', 'InConsultation')
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE appointments DROP CONSTRAINT IF EXISTS ex_appointments_staff_slot;");

            // btree_gist is left installed: dropping an extension other objects may depend on is a
            // worse outcome than leaving one behind.
        }
    }
}

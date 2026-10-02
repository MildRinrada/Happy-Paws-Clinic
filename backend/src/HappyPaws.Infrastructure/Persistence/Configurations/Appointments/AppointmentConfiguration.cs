using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Appointments;

internal sealed class AppointmentConfiguration : EntityConfiguration<Appointment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.Property(appointment => appointment.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(appointment => appointment.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(appointment => appointment.PetId).HasMaxLength(40).IsRequired();
        builder.Property(appointment => appointment.ServiceId).HasMaxLength(40).IsRequired();
        builder.Property(appointment => appointment.StaffId).HasMaxLength(40);
        builder.Property(appointment => appointment.BookedByStaffId).HasMaxLength(40);
        builder.Property(appointment => appointment.SourceHoldId).HasMaxLength(40);
        builder.Property(appointment => appointment.CancelledByUserId).HasMaxLength(40);
        builder.Property(appointment => appointment.Reason).HasMaxLength(1000);
        builder.Property(appointment => appointment.CancellationReason).HasMaxLength(500);
        builder.Property(appointment => appointment.SeriesId).HasMaxLength(40);

        // Every appointment made before types existed is a regular one; the migration's default
        // says so for the rows already there.
        builder.Property(appointment => appointment.TypeCode)
            .HasMaxLength(30)
            .HasDefaultValue(AppointmentTypeCatalog.DefaultCode)
            .IsRequired();

        // A course is read back together.
        builder.HasIndex(appointment => appointment.SeriesId).HasFilter("series_id IS NOT NULL");

        // Stored as the contract's own strings rather than as ordinals. An enum written as an
        // integer makes the table unreadable in psql and silently reorders if a member is ever
        // inserted in the middle; the state machine is part of the API, so it is stored as text.
        builder.Property(appointment => appointment.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        // The collision query: this vet, this branch, this window. Leading with branch and staff
        // keeps every booking check on an index rather than on the day's whole calendar.
        builder.HasIndex(appointment => new
        {
            appointment.BranchId,
            appointment.StaffId,
            appointment.StartsAt,
        });

        // GET /staff/appointments?date=&status= — the board, ordered by start time.
        builder.HasIndex(appointment => new { appointment.BranchId, appointment.StartsAt, appointment.Status });

        // GET /portal/appointments, both directions.
        builder.HasIndex(appointment => new { appointment.CustomerId, appointment.StartsAt });

        builder.HasIndex(appointment => new { appointment.PetId, appointment.StartsAt });

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(appointment => appointment.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(appointment => appointment.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(appointment => appointment.PetId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ClinicService>().WithMany()
            .HasForeignKey(appointment => appointment.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

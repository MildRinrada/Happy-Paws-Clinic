using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Queueing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Queueing;

internal sealed class QueueTicketConfiguration : EntityConfiguration<QueueTicket>
{
    protected override void ConfigureEntity(EntityTypeBuilder<QueueTicket> builder)
    {
        builder.ToTable("queue_tickets");

        builder.Property(ticket => ticket.Number).HasMaxLength(16).IsRequired();
        builder.Property(ticket => ticket.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(ticket => ticket.AppointmentId).HasMaxLength(40);
        builder.Property(ticket => ticket.PetId).HasMaxLength(40).IsRequired();
        builder.Property(ticket => ticket.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(ticket => ticket.ServiceId).HasMaxLength(40).IsRequired();
        builder.Property(ticket => ticket.StaffId).HasMaxLength(40);
        builder.Property(ticket => ticket.RoomId).HasMaxLength(60);
        builder.Property(ticket => ticket.CalledByStaffId).HasMaxLength(40);

        builder.Property(ticket => ticket.ServiceDate).HasColumnType("date").IsRequired();

        builder.Property(ticket => ticket.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        // Stored as its integer value, unlike Status. The board sorts on this column, and a text
        // enum would sort alphabetically: 'Emergency' < 'Normal' < 'Urgent', which puts an urgent
        // case behind a routine one.
        builder.Property(ticket => ticket.Priority)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(QueuePriority.Normal);

        builder.Property(ticket => ticket.PriorityReason).HasMaxLength(300);
        builder.Property(ticket => ticket.PrioritySetByStaffId).HasMaxLength(40);
        builder.Property(ticket => ticket.LeftReason).HasMaxLength(300);

        // The daily reset, enforced rather than assumed: two tickets cannot share a number at one
        // branch on one day, whatever the allocator does.
        builder.HasIndex(ticket => new { ticket.BranchId, ticket.ServiceDate, ticket.Number })
            .IsUnique();

        // One visit, one ticket. Partial because counter-issued tickets carry no appointment.
        builder.HasIndex(ticket => ticket.AppointmentId)
            .IsUnique()
            .HasFilter("appointment_id IS NOT NULL");

        // The board query and the "how many ahead of me" count both read exactly this predicate,
        // and a waiting-room screen re-reads it every few seconds.
        builder.HasIndex(ticket => new { ticket.BranchId, ticket.ServiceDate, ticket.Status });

        // The number allocator takes MAX(sequence) under an advisory lock on every check-in.
        builder.HasIndex(ticket => new { ticket.BranchId, ticket.ServiceDate, ticket.Sequence });

        // Covers the board's filter and carries the columns its ordering reads. It does not remove
        // the sort — the "skipped or not" term is a computed expression and priority sorts the
        // other way — but a day at one branch is tens of rows, so the sort is free and the filter
        // is what matters.
        builder.HasIndex(ticket => new
        {
            ticket.BranchId,
            ticket.ServiceDate,
            ticket.Priority,
            ticket.SkippedAt,
            ticket.Sequence,
        });

        builder.HasIndex(ticket => new { ticket.CustomerId, ticket.ServiceDate });

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(ticket => ticket.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Appointment>().WithMany()
            .HasForeignKey(ticket => ticket.AppointmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

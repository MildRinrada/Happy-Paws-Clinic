using HappyPaws.Domain.Clinics;
using HappyPaws.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Clinics;

// The clinic's one-row tables. Each carries the same check constraint booking_settings does, for
// the same reason: "there is exactly one" is enforced by the database, not remembered by a query.

internal sealed class ClinicProfileConfiguration : EntityConfiguration<ClinicProfile>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClinicProfile> builder)
    {
        builder.ToTable("clinic_profile");

        builder.Property(profile => profile.Name).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.LogoUrl).HasMaxLength(1000);

        // docs/contract-extensions.md § 50. The column for the "show the name" switch has no
        // default in the model: a default of true would make EF leave a false out of the first
        // insert. The migration fills existing rows with true instead.
        builder.Property(profile => profile.TabIconUrl).HasMaxLength(1000);
        builder.Property(profile => profile.ShowNameWithLogo);
        builder.Property(profile => profile.BrandColor).HasMaxLength(7);
        builder.Property(profile => profile.LicenseNo).HasMaxLength(64);
        builder.Property(profile => profile.OperatorName).HasMaxLength(200);

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_clinic_profile_singleton",
            $"id = '{ClinicProfile.SingletonId}'"));
    }
}

internal sealed class OpeningHoursConfiguration : EntityConfiguration<OpeningHours>
{
    protected override void ConfigureEntity(EntityTypeBuilder<OpeningHours> builder)
    {
        builder.ToTable("opening_hours");

        // The week is one value, written whole by PUT /clinic/hours and read whole by the public
        // site. One jsonb column keeps it under one version, which seven rows could not.
        builder.Property(hours => hours.Days)
            .HasField("_days")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .IsRequired();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_opening_hours_singleton",
            $"id = '{OpeningHours.SingletonId}'"));
    }
}

internal sealed class ClinicHolidayConfiguration : EntityConfiguration<ClinicHoliday>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClinicHoliday> builder)
    {
        builder.ToTable("clinic_holidays");

        builder.Property(holiday => holiday.Date).HasColumnType("date").IsRequired();
        builder.Property(holiday => holiday.Name).HasMaxLength(200).IsRequired();
        builder.Property(holiday => holiday.BranchId).HasMaxLength(40);

        // One closure per day per branch, where "every branch" counts as a branch of its own.
        // NULLS NOT DISTINCT is what makes the second half true: by default two rows with a null
        // branch are not equal to each other, and the same clinic-wide holiday could be declared
        // twice.
        builder.HasIndex(holiday => new { holiday.Date, holiday.BranchId })
            .IsUnique()
            .AreNullsDistinct(false);

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(holiday => holiday.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ClinicContactConfiguration : EntityConfiguration<ClinicContact>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClinicContact> builder)
    {
        builder.ToTable("clinic_contacts");

        builder.Property(contact => contact.Kind).HasMaxLength(20).IsRequired();
        builder.Property(contact => contact.Label).HasMaxLength(ClinicContact.LabelMax).IsRequired();
        builder.Property(contact => contact.Value).HasMaxLength(ClinicContact.ValueMax).IsRequired();
        builder.Property(contact => contact.Url).HasMaxLength(ClinicContact.UrlMax);
        builder.Property(contact => contact.EmbedUrl).HasMaxLength(ClinicContact.UrlMax);
        builder.Property(contact => contact.BranchId).HasMaxLength(40);
        builder.Property(contact => contact.SortOrder).IsRequired();

        builder.HasIndex(contact => contact.SortOrder);

        // A branch is closed, never deleted, so Restrict costs nothing and keeps an entry from
        // pointing at a branch that is not there.
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(contact => contact.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class QueueSettingsConfiguration : EntityConfiguration<QueueSettings>
{
    protected override void ConfigureEntity(EntityTypeBuilder<QueueSettings> builder)
    {
        builder.ToTable("queue_settings");

        builder.Property(settings => settings.NumberPrefix).HasMaxLength(3).IsRequired();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_queue_settings_singleton",
            $"id = '{QueueSettings.SingletonId}'"));
    }
}

internal sealed class MedicalRecordSettingsConfiguration : EntityConfiguration<MedicalRecordSettings>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MedicalRecordSettings> builder)
    {
        builder.ToTable("medical_record_settings");

        builder.PrimitiveCollection(settings => settings.RequiredFields)
            .HasColumnName("required_fields")
            .IsRequired();

        builder.PrimitiveCollection(settings => settings.ExaminationSystems)
            .HasColumnName("examination_systems")
            .IsRequired();

        builder.PrimitiveCollection(settings => settings.OwnerVisibleSections)
            .HasColumnName("owner_visible_sections")
            .IsRequired();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_medical_record_settings_singleton",
            $"id = '{MedicalRecordSettings.SingletonId}'"));
    }
}

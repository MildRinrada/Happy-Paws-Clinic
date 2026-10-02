using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Patients;

internal sealed class PetConfiguration : EntityConfiguration<Pet>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("pets");

        builder.Property(pet => pet.HospitalNumber).HasMaxLength(40).IsRequired();
        builder.Property(pet => pet.Name).HasMaxLength(120).IsRequired();
        builder.Property(pet => pet.Species).HasMaxLength(60).IsRequired();
        builder.Property(pet => pet.Breed).HasMaxLength(120);
        builder.Property(pet => pet.PrimaryCustomerId).HasMaxLength(40).IsRequired();
        builder.Property(pet => pet.MicrochipNumber).HasMaxLength(40);

        // The owner's half of the record — PATCH /portal/pets/{id}.
        builder.Property(pet => pet.Nickname).HasMaxLength(60);
        builder.Property(pet => pet.PhotoDocumentId).HasMaxLength(40);
        builder.Property(pet => pet.Diet).HasMaxLength(500);
        builder.Property(pet => pet.Behaviour).HasMaxLength(1_000);

        builder.HasIndex(pet => pet.HospitalNumber).IsUnique();
        builder.HasIndex(pet => pet.PrimaryCustomerId);

        // A chip identifies exactly one animal in the world. Partial, because most patients have
        // none and they must not all collide on null.
        builder.HasIndex(pet => pet.MicrochipNumber)
            .IsUnique()
            .HasFilter("microchip_number IS NOT NULL");

        // The registry's ?q= runs an infix match on the patient's name — same reasoning as the
        // customer's display name.
        builder.HasIndex(pet => pet.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("ix_pets_name_trgm");

        // The register's default ordering, newest first.
        builder.HasIndex(pet => pet.CreatedAt).IsDescending();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(pet => pet.PrimaryCustomerId)
            // Restrict, not Cascade: deleting a customer must never take a patient's clinical
            // history with it. Records outlive the relationship that created them.
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PetOwnershipConfiguration : EntityConfiguration<PetOwnership>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PetOwnership> builder)
    {
        builder.ToTable("pet_ownerships");

        builder.Property(link => link.PetId).HasMaxLength(40).IsRequired();
        builder.Property(link => link.CustomerId).HasMaxLength(40).IsRequired();

        // Every portal request runs this lookup, so it is indexed the way it is queried:
        // "is this customer a live carer of this pet?"
        builder.HasIndex(link => new { link.PetId, link.CustomerId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL");

        builder.HasIndex(link => new { link.CustomerId, link.RevokedAt });

        builder.HasOne<Pet>()
            .WithMany()
            .HasForeignKey(link => link.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(link => link.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

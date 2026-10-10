using HappyPaws.Domain.Common;
using HappyPaws.Domain.Patients;

namespace HappyPaws.UnitTests.Domain.Patients;

/// <summary>
/// Staff's edit of the identity card (contract-extensions § 28) and taking back a death recorded
/// by mistake (§ 29).
/// </summary>
public class PetIdentityTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 3, 0, 0, TimeSpan.Zero);

    private static Pet Luna() => Pet.Create("HN-2026-0001", "ลูน่า", "สุนัข", "cus_1", "ชิสุ", "900000000000001");

    [Fact]
    public void A_new_patient_is_of_unknown_sex_with_no_birth_date_and_unknown_neuter_status()
    {
        var pet = Luna();

        Assert.Equal(PetSex.Unknown, pet.Sex);
        Assert.Null(pet.BirthDate);
        Assert.False(pet.BirthDateEstimated);
        Assert.Null(pet.Neutered);
    }

    [Fact]
    public void Only_what_is_sent_changes()
    {
        var pet = Luna();

        pet.Revise(new PetRevision(Sex: PetSex.Female, BirthDate: new DateOnly(2022, 3, 1), Neutered: true), Today);
        pet.Revise(new PetRevision(Name: "ลูน่า ทองดี"), Today);

        Assert.Equal("ลูน่า ทองดี", pet.Name);
        Assert.Equal("สุนัข", pet.Species);
        Assert.Equal("ชิสุ", pet.Breed);
        Assert.Equal(PetSex.Female, pet.Sex);
        Assert.Equal(new DateOnly(2022, 3, 1), pet.BirthDate);
        Assert.True(pet.Neutered);
    }

    [Fact]
    public void An_empty_string_clears_breed_and_chip_but_not_name_or_species()
    {
        var pet = Luna();

        pet.Revise(new PetRevision(Breed: "", MicrochipNumber: "  "), Today);

        Assert.Null(pet.Breed);
        Assert.Null(pet.MicrochipNumber);
        Assert.Throws<DomainException>(() => pet.Revise(new PetRevision(Name: " "), Today));
        Assert.Throws<DomainException>(() => pet.Revise(new PetRevision(Species: ""), Today));
    }

    [Fact]
    public void A_birth_date_in_the_future_or_before_1900_is_refused_and_today_is_fine()
    {
        var pet = Luna();

        Assert.Throws<DomainException>(() => pet.Revise(new PetRevision(BirthDate: Today.AddDays(1)), Today));
        Assert.Throws<DomainException>(() => pet.Revise(new PetRevision(BirthDate: new DateOnly(1899, 12, 31)), Today));

        pet.Revise(new PetRevision(BirthDate: Today), Today);
        Assert.Equal(Today, pet.BirthDate);
    }

    [Fact]
    public void A_refused_revision_changes_nothing()
    {
        var pet = Luna();

        Assert.Throws<DomainException>(() =>
            pet.Revise(new PetRevision(Name: "ชื่อใหม่", BirthDate: Today.AddDays(3)), Today));

        Assert.Equal("ลูน่า", pet.Name);
        Assert.Null(pet.BirthDate);
    }

    [Fact]
    public void An_estimated_birth_date_needs_a_birth_date_and_clearing_the_date_clears_the_estimate()
    {
        var pet = Luna();

        Assert.Throws<DomainException>(() => pet.Revise(new PetRevision(BirthDateEstimated: true), Today));

        pet.Revise(new PetRevision(BirthDate: new DateOnly(2020, 1, 1), BirthDateEstimated: true), Today);
        Assert.True(pet.BirthDateEstimated);

        pet.Revise(new PetRevision(ClearBirthDate: true), Today);
        Assert.Null(pet.BirthDate);
        Assert.False(pet.BirthDateEstimated);
    }

    [Fact]
    public void A_new_exact_date_without_the_flag_is_no_longer_an_estimate()
    {
        var pet = Luna();
        pet.Revise(new PetRevision(BirthDate: new DateOnly(2020, 1, 1), BirthDateEstimated: true), Today);

        pet.Revise(new PetRevision(BirthDate: new DateOnly(2020, 4, 12)), Today);

        Assert.False(pet.BirthDateEstimated);
    }

    [Fact]
    public void Neuter_status_can_go_back_to_unknown()
    {
        var pet = Luna();
        pet.Revise(new PetRevision(Neutered: false), Today);

        pet.Revise(new PetRevision(ClearNeutered: true), Today);

        Assert.Null(pet.Neutered);
    }

    [Fact]
    public void Sending_a_value_and_clearing_it_at_once_is_refused()
    {
        var pet = Luna();

        Assert.Throws<DomainException>(() => pet.Revise(new PetRevision(BirthDate: Today, ClearBirthDate: true), Today));
        Assert.Throws<DomainException>(() => pet.Revise(new PetRevision(Neutered: true, ClearNeutered: true), Today));
    }

    [Fact]
    public void A_closed_record_takes_identity_corrections_but_not_a_change_of_neuter_status()
    {
        var pet = Luna();
        pet.MarkDeceased(Now);

        pet.Revise(new PetRevision(Name: "ลูน่า", Breed: "พุดเดิ้ล", Sex: PetSex.Female, BirthDate: new DateOnly(2015, 5, 5)), Today);
        Assert.Equal("พุดเดิ้ล", pet.Breed);

        var refused = Assert.Throws<StateConflictException>(() => pet.Revise(new PetRevision(Neutered: true), Today));
        Assert.Equal("deceased", refused.CurrentState);

        // Sending the value it already has is not a change.
        pet.Revise(new PetRevision(Neutered: null), Today);
    }

    [Fact]
    public void Undoing_a_death_opens_the_record_and_remembers_when()
    {
        var pet = Luna();
        var died = Now.AddDays(-3);
        pet.MarkDeceased(died);

        var undone = pet.UndoDeceased(Now);

        Assert.Equal(died, undone);
        Assert.False(pet.IsDeceased);
        Assert.Null(pet.DeceasedAt);
        Assert.Equal(Now, pet.DeceasedUndoneAt);

        // The owner's own edit works again.
        pet.UpdateOwnerProfile(nickname: "ลูลู่");
        Assert.Equal("ลูลู่", pet.Nickname);
    }

    [Fact]
    public void A_living_patient_has_no_death_to_undo()
    {
        var refused = Assert.Throws<StateConflictException>(() => Luna().UndoDeceased(Now));

        Assert.Equal("alive", refused.CurrentState);
    }

    [Fact]
    public void After_an_undo_the_patient_can_be_recorded_deceased_again()
    {
        var pet = Luna();
        pet.MarkDeceased(Now.AddDays(-3));
        pet.UndoDeceased(Now.AddDays(-1));

        pet.MarkDeceased(Now);

        Assert.Equal(Now, pet.DeceasedAt);
        Assert.Equal(Now.AddDays(-1), pet.DeceasedUndoneAt);
    }
}

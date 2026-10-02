using HappyPaws.Domain.Common;
using HappyPaws.Domain.Laboratory;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.UnitTests.Domain.Patients;

/// <summary>Vaccinations, lab results and the duplicate-therapy check.</summary>
public class ClinicalRecordsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 2);

    private static Vaccination Dose(int? validForDays = null, DateOnly? nextDueOn = null, DateOnly? vialExpiresOn = null) =>
        Vaccination.Record("pet_1", "Rabies", "LOT-1", vialExpiresOn ?? Today.AddYears(1), Now, Today, "stf_1", nextDueOn, validForDays);

    [Fact]
    public void Without_instructions_the_next_dose_is_a_year_on()
    {
        Assert.Equal(Today.AddDays(365), Dose().NextDueOn);
    }

    [Fact]
    public void Zero_days_of_cover_means_no_further_dose()
    {
        Assert.Null(Dose(validForDays: 0).NextDueOn);
    }

    [Fact]
    public void An_explicit_due_date_wins_but_cannot_be_on_or_before_the_dose()
    {
        Assert.Equal(Today.AddDays(21), Dose(nextDueOn: Today.AddDays(21)).NextDueOn);
        Assert.Throws<DomainException>(() => Dose(nextDueOn: Today));
    }

    [Fact]
    public void A_vial_past_its_date_is_refused()
    {
        var exception = Assert.Throws<DomainException>(() => Dose(vialExpiresOn: Today.AddDays(-1)));

        Assert.Equal("invalid_vaccination", exception.Code);
    }

    [Theory]
    [InlineData(5, "low")]
    [InlineData(50, "normal")]
    [InlineData(500, "high")]
    public void A_number_is_flagged_by_its_own_range(int value, string expected)
    {
        var flag = LabResult.FlagFor(new LabResultInput("ALT", value, ReferenceLow: 10, ReferenceHigh: 100, Abnormal: false));

        // The range decides, even against what the caller said.
        Assert.Equal(expected, flag.ToString()!.ToLowerInvariant());
    }

    [Fact]
    public void Text_without_a_judgement_has_no_flag()
    {
        Assert.Null(LabResult.FlagFor(new LabResultInput("Colour", ValueText: "เหลือง")));
        Assert.Equal(LabFlag.Abnormal, LabResult.FlagFor(new LabResultInput("Ag", ValueText: "positive", Abnormal: true)));
    }

    [Fact]
    public void New_results_put_a_reviewed_request_back_in_front_of_a_vet()
    {
        var request = LabRequest.Create("pet_1", "brn_1", "stf_1", Now, LabDestination.InHouse, [new LabTest("CBC")]);

        request.RecordResults([new LabResultInput("WBC", 10)], "stf_2", Now);
        request.Review("stf_1", Now);
        request.RecordResults([new LabResultInput("RBC", 5)], "stf_2", Now.AddHours(1));

        Assert.Equal(LabRequestStatus.Resulted, request.Status);
        Assert.Null(request.ReviewedAt);
    }

    [Fact]
    public void A_request_with_no_results_cannot_be_reviewed()
    {
        var request = LabRequest.Create("pet_1", "brn_1", "stf_1", Now, LabDestination.InHouse, [new LabTest("CBC")]);

        Assert.Throws<StateConflictException>(() => request.Review("stf_1", Now));
    }

    [Fact]
    public void The_same_drug_in_another_strength_on_an_overlapping_course_is_a_duplicate()
    {
        var first = Prescription.Write("con_1", "pet_1", "stf_1", Now, "Amoxicillin 250mg", "1", "bid", 7, 14);
        var second = Prescription.Write("con_2", "pet_1", "stf_1", Now.AddDays(3), "AMOXICILLIN 500 mg", "1", "bid", 7, 14);
        var later = Prescription.Write("con_3", "pet_1", "stf_1", Now.AddDays(30), "Amoxicillin 250mg", "1", "bid", 7, 14);

        Assert.True(first.DuplicatesTherapyOf(second));
        Assert.False(first.DuplicatesTherapyOf(later));
        Assert.False(first.DuplicatesTherapyOf(first));
    }
}

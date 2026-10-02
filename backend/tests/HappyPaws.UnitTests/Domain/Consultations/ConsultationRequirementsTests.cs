using HappyPaws.Domain.Consultations;

namespace HappyPaws.UnitTests.Domain.Consultations;

public class ConsultationRequirementsTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 10, 0, 0, TimeSpan.FromHours(7));

    private static Consultation OpenCase() =>
        Consultation.Open("brn_1", "pet_1", "cus_1", "stf_vet", Now);

    [Fact]
    public void A_clinic_that_requires_nothing_extra_finds_nothing_missing()
    {
        Assert.Empty(ConsultationRequirements.MissingFrom(OpenCase(), []));
    }

    [Fact]
    public void Missing_parts_are_named_as_the_request_bodies_name_them()
    {
        var consultation = OpenCase();

        var missing = ConsultationRequirements.MissingFrom(
            consultation,
            [ConsultationRequirements.Vitals, ConsultationRequirements.Plan, ConsultationRequirements.Examination]);

        Assert.Equal(["vitals", "plan", "examination"], missing);
    }

    [Fact]
    public void A_part_that_has_been_filled_in_is_no_longer_missing()
    {
        var consultation = OpenCase();

        consultation.AddVitals("stf_asst", Now, weightKg: 4.2m);
        consultation.UpdateDraft(plan: "นัดติดตามผล 7 วัน");
        consultation.ReplaceExamination([("skin", true, null)]);

        Assert.Empty(ConsultationRequirements.MissingFrom(consultation, ConsultationRequirements.All
            .Except([
                ConsultationRequirements.ChiefComplaint, ConsultationRequirements.Subjective,
                ConsultationRequirements.Objective, ConsultationRequirements.Assessment,
            ])));
    }

    [Fact]
    public void A_field_cleared_back_to_empty_counts_as_missing_again()
    {
        var consultation = OpenCase();
        consultation.UpdateDraft(plan: "แผนการรักษา");

        consultation.UpdateDraft(plan: string.Empty);

        Assert.Equal(
            [ConsultationRequirements.Plan],
            ConsultationRequirements.MissingFrom(consultation, [ConsultationRequirements.Plan]));
    }
}

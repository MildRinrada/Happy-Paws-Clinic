using HappyPaws.Domain.Common;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.UnitTests.Domain.Consultations;

/// <summary>
/// The rules the consultation aggregate enforces, and the one it exists to enforce above all: once
/// the case is closed, nothing in it can be edited again.
/// </summary>
public class ConsultationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 9, 0, 0, TimeSpan.FromHours(7));

    private static Consultation Open() =>
        Consultation.Open("brn_1", "pet_luna", "cus_1", "stf_204", Now, appointmentId: "apt_1");

    private static Consultation Closed()
    {
        var consultation = Open();
        consultation.AddDiagnosis("ผิวหนังอักเสบ", isPrimary: true);
        consultation.Close(Now.AddHours(1), "rec_1");

        return consultation;
    }

    [Fact]
    public void The_first_diagnosis_is_primary_even_when_the_caller_did_not_say_so()
    {
        var consultation = Open();

        consultation.AddDiagnosis("หูอักเสบ", isPrimary: false);

        // Otherwise a case can end up with only secondary diagnoses and no principal one, which is
        // a case nobody can code or summarise.
        Assert.True(consultation.Diagnoses.Single().IsPrimary);
    }

    [Fact]
    public void A_new_primary_diagnosis_demotes_the_previous_one()
    {
        var consultation = Open();

        consultation.AddDiagnosis("หูอักเสบ", isPrimary: true);
        consultation.AddDiagnosis("ผิวหนังอักเสบ", isPrimary: true);

        Assert.Single(consultation.Diagnoses, diagnosis => diagnosis.IsPrimary);
        Assert.Equal("ผิวหนังอักเสบ", consultation.PrimaryDiagnosis!.Description);
    }

    [Fact]
    public void Re_posting_the_examination_replaces_it_rather_than_appending()
    {
        var consultation = Open();

        consultation.ReplaceExamination([("ผิวหนัง", false, "ผื่นแดงบริเวณท้อง"), ("ช่องปาก", true, null)]);
        consultation.ReplaceExamination([("ผิวหนัง", true, null)]);

        // The contract describes one form covering every system, so a second submission is the
        // corrected form. Appending would leave two answers for one system.
        Assert.Equal("ผิวหนัง", consultation.Findings.Single().BodySystem);
        Assert.True(consultation.Findings.Single().IsNormal);
    }

    [Fact]
    public void The_same_body_system_cannot_be_answered_twice_in_one_form()
    {
        var consultation = Open();

        Assert.Throws<DomainException>(() =>
            consultation.ReplaceExamination([("ผิวหนัง", true, null), ("ผิวหนัง", false, "ผื่น")]));
    }

    [Fact]
    public void An_abnormal_finding_without_a_note_is_refused()
    {
        var consultation = Open();

        // "Abnormal" alone is a flag with no clinical content — the next vet to read the file
        // cannot act on it.
        Assert.Throws<DomainException>(() => consultation.ReplaceExamination([("ช่องท้อง", false, null)]));
    }

    [Fact]
    public void A_case_cannot_be_closed_without_a_diagnosis()
    {
        var consultation = Open();

        Assert.Throws<DomainException>(() => consultation.Close(Now.AddHours(1), "rec_1"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public void An_implausible_weight_is_refused(decimal weight)
    {
        var consultation = Open();

        // A transposed digit here becomes a drug dose.
        Assert.Throws<DomainException>(() => consultation.AddVitals("stf_1", Now, weightKg: weight));
    }

    [Fact]
    public void Vitals_accumulate_and_the_latest_is_the_current_one()
    {
        var consultation = Open();

        consultation.AddVitals("stf_1", Now, weightKg: 8.2m);
        consultation.AddVitals("stf_1", Now.AddMinutes(20), weightKg: 8.4m);

        Assert.Equal(2, consultation.Vitals.Count);
        Assert.Equal(8.4m, consultation.LatestVitals!.WeightKg);
    }

    [Fact]
    public void Treatment_charges_are_the_sum_of_the_lines()
    {
        var consultation = Open();

        consultation.AddTreatment("ทำแผล", 2, 30_000, "stf_1", Now);
        consultation.AddTreatment("ตัดเล็บ", 1, 15_000, "stf_1", Now);

        Assert.Equal(75_000, consultation.TreatmentCharges.Satang);
    }

    [Fact]
    public void Closing_records_the_signed_record_and_the_time()
    {
        var consultation = Closed();

        Assert.Equal(ConsultationStatus.Closed, consultation.Status);
        Assert.Equal("rec_1", consultation.MedicalRecordId);
        Assert.NotNull(consultation.ClosedAt);
    }

    [Fact]
    public void Every_write_on_a_closed_case_answers_record_locked_rather_than_state_conflict()
    {
        var consultation = Closed();

        // 423, not 409. The difference matters to the caller: 409 invites a retry, 423 says the
        // document is signed and the correction has to go through an amendment.
        Assert.Throws<RecordLockedException>(() => consultation.UpdateDraft(assessment: "แก้ทีหลัง"));
        Assert.Throws<RecordLockedException>(() => consultation.AddVitals("stf_1", Now, weightKg: 8m));
        Assert.Throws<RecordLockedException>(() => consultation.ReplaceExamination([("ผิวหนัง", true, null)]));
        Assert.Throws<RecordLockedException>(() => consultation.AddDiagnosis("อีกโรค", false));
        Assert.Throws<RecordLockedException>(() => consultation.AddTreatment("ทำแผล", 1, 100, "stf_1", Now));
        Assert.Throws<RecordLockedException>(() =>
            consultation.AddPrescription("stf_1", Now, "Amoxicillin", "1 เม็ด", "วันละ 2 ครั้ง", 7, 14));
        Assert.Throws<RecordLockedException>(() => consultation.Close(Now, "rec_2"));
    }

    [Fact]
    public void Autosave_leaves_untouched_fields_alone_and_clears_only_what_was_sent()
    {
        var consultation = Open();

        consultation.UpdateDraft(subjective: "ซึม ไม่กินอาหาร 2 วัน", plan: "ให้ยา 7 วัน");
        consultation.UpdateDraft(assessment: "สงสัยติดเชื้อ");

        // Absence means "not sent", not "clear it" — otherwise every autosave would wipe whatever
        // the vet was not typing into at that second.
        Assert.Equal("ซึม ไม่กินอาหาร 2 วัน", consultation.Subjective);
        Assert.Equal("ให้ยา 7 วัน", consultation.Plan);
        Assert.Equal("สงสัยติดเชื้อ", consultation.Assessment);

        consultation.UpdateDraft(plan: "   ");
        Assert.Null(consultation.Plan);
    }
}

using HappyPaws.Domain.Common;
using HappyPaws.Domain.MedicalRecords;

namespace HappyPaws.UnitTests.Domain.MedicalRecords;

/// <summary>
/// Permanence, and what "แก้ไขแบบมีร่องรอย · เก็บฉบับเดิมไว้เสมอ" means when written down as code.
/// </summary>
public class MedicalRecordTests
{
    private static readonly DateTimeOffset SignedAt = new(2026, 8, 26, 11, 0, 0, TimeSpan.FromHours(7));

    private const string Original = """{"assessment":"สงสัยติดเชื้อผิวหนัง","plan":"ให้ยา 7 วัน"}""";

    private static MedicalRecord Sign(string content = Original) =>
        MedicalRecord.Sign("con_771", "pet_luna", "cus_1", "brn_1", content, "stf_204", SignedAt);

    [Fact]
    public void A_signed_record_verifies_against_its_own_hash()
    {
        var record = Sign();

        Assert.Equal(MedicalRecordStatus.Signed, record.Status);
        Assert.True(record.VerifyIntegrity());
        Assert.NotEmpty(record.ContentHash);
    }

    [Fact]
    public void Two_records_signed_by_different_vets_over_the_same_content_hash_differently()
    {
        var mine = Sign();
        var theirs = MedicalRecord.Sign(
            "con_771", "pet_luna", "cus_1", "brn_1", Original, "stf_999", SignedAt);

        // The signer is part of what is hashed. Otherwise the hash would attest to the text alone
        // and say nothing about who stands behind it.
        Assert.NotEqual(mine.ContentHash, theirs.ContentHash);
    }

    [Fact]
    public void The_hash_survives_the_precision_a_database_actually_stores()
    {
        var precise = SignedAt.AddTicks(3_674);

        // What PostgreSQL gives back: timestamptz keeps microseconds, so the sub-microsecond ticks
        // are gone. One microsecond is ten ticks.
        var asStored = new DateTimeOffset(precise.Ticks - (precise.Ticks % 10), precise.Offset);

        var signed = MedicalRecord.Sign("con_771", "pet_luna", "cus_1", "brn_1", Original, "stf_204", precise);
        var reloaded = MedicalRecord.Sign("con_771", "pet_luna", "cus_1", "brn_1", Original, "stf_204", asStored);

        // DateTimeOffset counts 100-nanosecond ticks; PostgreSQL timestamptz stores microseconds.
        // If the hash covered the full tick precision, every record would come back from the
        // database reporting itself as tampered with.
        Assert.Equal(signed.ContentHash, reloaded.ContentHash);
    }

    [Fact]
    public void An_empty_document_cannot_be_signed()
    {
        Assert.Throws<DomainException>(() =>
            MedicalRecord.Sign("con_771", "pet_luna", "cus_1", "brn_1", "  ", "stf_204", SignedAt));
    }

    [Fact]
    public void An_amendment_leaves_the_original_untouched_and_becomes_the_current_version()
    {
        var record = Sign();
        const string corrected = """{"assessment":"ยืนยันติดเชื้อผิวหนัง","plan":"ให้ยา 7 วัน"}""";

        record.Amend(corrected, """{"assessment":{"from":"ก","to":"ข"}}""", "ผลแล็บยืนยัน", "stf_204", SignedAt.AddDays(1));

        Assert.Equal(MedicalRecordStatus.Amended, record.Status);
        Assert.Equal(Original, record.ContentJson);
        Assert.Equal(corrected, record.CurrentContentJson);
        Assert.Single(record.Amendments);
    }

    [Fact]
    public void The_original_still_verifies_after_being_amended()
    {
        var record = Sign();

        record.Amend("""{"assessment":"แก้แล้ว"}""", "{}", "พิมพ์ผิด", "stf_204", SignedAt.AddDays(1));

        // The signature attests to what was signed. An amendment adds a version; it does not
        // retroactively change what the signer put their name to.
        Assert.True(record.VerifyIntegrity());
    }

    [Fact]
    public void A_second_amendment_stacks_and_the_newest_wins()
    {
        var record = Sign();

        record.Amend("""{"plan":"ครั้งที่หนึ่ง"}""", "{}", "เหตุผลแรก", "stf_204", SignedAt.AddDays(1));
        record.Amend("""{"plan":"ครั้งที่สอง"}""", "{}", "เหตุผลที่สอง", "stf_999", SignedAt.AddDays(2));

        Assert.Equal(2, record.Amendments.Count);
        Assert.Equal("""{"plan":"ครั้งที่สอง"}""", record.CurrentContentJson);
        // Still amended, not "twice amended" — the count is in the list, not in the status.
        Assert.Equal(MedicalRecordStatus.Amended, record.Status);
    }

    [Fact]
    public void An_amendment_without_a_reason_is_refused()
    {
        var record = Sign();

        // Read back years later, an unexplained change is indistinguishable from tampering.
        Assert.Throws<DomainException>(() =>
            record.Amend("""{"plan":"x"}""", "{}", "   ", "stf_204", SignedAt.AddDays(1)));
    }
}

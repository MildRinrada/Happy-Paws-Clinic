using HappyPaws.Domain.Common;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.UnitTests.Domain.Laboratory;

/// <summary>
/// <c>docs/contract-extensions.md § 52</c>: a value is corrected or struck out, never overwritten
/// without trace, and the request's reading follows.
/// </summary>
public class LabResultCorrectionTests
{
    private static readonly DateTimeOffset Requested = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Requested.AddHours(3);

    private static LabRequest Resulted()
    {
        var request = LabRequest.Create("pet_1", "brn_1", "stf_vet", Requested, LabDestination.InHouse, [new LabTest("CBC")]);
        request.RecordResults(
            [
                new LabResultInput("ALT", 180m, Unit: "U/L", ReferenceLow: 10m, ReferenceHigh: 100m),
                new LabResultInput("HCT", 22m, Unit: "%", ReferenceLow: 37m, ReferenceHigh: 55m),
            ],
            "stf_lab",
            Requested.AddHours(1));

        return request;
    }

    private static LabResult Value(LabRequest request, string analyte) => request.Results.Single(result => result.Analyte == analyte);

    [Fact]
    public void A_correction_keeps_the_old_value_and_flags_the_new_one()
    {
        var request = Resulted();
        var alt = Value(request, "ALT");

        var before = request.AmendResult(alt, new LabResultInput("", 80m, Unit: "U/L", ReferenceLow: 10m, ReferenceHigh: 100m), " พิมพ์ผิด ", "stf_lab", Later);

        Assert.Equal(180m, before.ValueNumeric);
        Assert.Equal("high", before.Flag);
        Assert.Equal("พิมพ์ผิด", before.Reason);
        Assert.Equal("ALT", alt.Analyte);
        Assert.Equal(80m, alt.ValueNumeric);
        Assert.Equal(LabFlag.Normal, alt.Flag);
        Assert.True(alt.IsAmended);
        Assert.Equal(Later, alt.AmendedAt);
        Assert.Equal("stf_lab", alt.AmendedByStaffId);
        Assert.Single(alt.PreviousValues);
        Assert.Equal(1, request.AbnormalCount);
    }

    [Fact]
    public void Every_correction_is_kept_in_order()
    {
        var request = Resulted();
        var alt = Value(request, "ALT");

        request.AmendResult(alt, new LabResultInput("ALT", 80m), "หนึ่ง", "stf_lab", Later);
        request.AmendResult(alt, new LabResultInput("ALT", 90m), "สอง", "stf_lab", Later.AddMinutes(5));

        Assert.Equal([180m, 80m], alt.PreviousValues.Select(value => value.ValueNumeric!.Value));
        Assert.Equal("สอง", alt.AmendReason);
    }

    [Fact]
    public void A_correction_of_read_results_puts_them_back_in_front_of_a_vet()
    {
        var request = Resulted();
        request.Review("stf_vet", Later, "ตรวจซ้ำ");

        request.AmendResult(Value(request, "ALT"), new LabResultInput("ALT", 80m), "พิมพ์ผิด", "stf_lab", Later.AddHours(1));

        Assert.Equal(LabRequestStatus.Resulted, request.Status);
        Assert.Equal(Later.AddHours(1), request.ResultedAt);
        Assert.Null(request.ReviewedAt);
        Assert.Null(request.ReviewedByStaffId);
        Assert.Null(request.ReviewNote);
    }

    [Fact]
    public void A_correction_of_unread_results_leaves_the_waiting_time_alone()
    {
        var request = Resulted();
        var resultedAt = request.ResultedAt;

        request.AmendResult(Value(request, "ALT"), new LabResultInput("ALT", 80m), "พิมพ์ผิด", "stf_lab", Later);

        Assert.Equal(LabRequestStatus.Resulted, request.Status);
        Assert.Equal(resultedAt, request.ResultedAt);
    }

    [Fact]
    public void A_voided_value_counts_towards_nothing_and_cannot_be_changed_again()
    {
        var request = Resulted();
        var hct = Value(request, "HCT");

        request.VoidResult(hct, "ผิดตัว", "stf_lab", Later);

        Assert.True(hct.IsVoided);
        Assert.False(hct.IsAbnormal);
        Assert.Equal(22m, hct.ValueNumeric);
        Assert.Equal(1, request.AbnormalCount);
        Assert.Throws<StateConflictException>(() => request.VoidResult(hct, "อีก", "stf_lab", Later));
        Assert.Throws<StateConflictException>(() => request.AmendResult(hct, new LabResultInput("HCT", 40m), "แก้", "stf_lab", Later));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_correction_needs_a_reason(string reason)
    {
        var request = Resulted();

        var error = Assert.Throws<DomainException>(() =>
            request.AmendResult(Value(request, "ALT"), new LabResultInput("ALT", 80m), reason, "stf_lab", Later));
        Assert.Equal("invalid_lab_result", error.Code);
        Assert.Throws<DomainException>(() => request.VoidResult(Value(request, "ALT"), reason, "stf_lab", Later));
    }

    [Fact]
    public void A_corrected_value_follows_the_rules_of_a_new_one()
    {
        var request = Resulted();
        var alt = Value(request, "ALT");

        Assert.Throws<DomainException>(() => request.AmendResult(alt, new LabResultInput("ALT"), "ไม่มีค่า", "stf_lab", Later));
        Assert.Throws<DomainException>(() =>
            request.AmendResult(alt, new LabResultInput("ALT", 5m, ReferenceLow: 10m, ReferenceHigh: 1m), "ช่วงกลับด้าน", "stf_lab", Later));

        // A refused correction changed nothing.
        Assert.Equal(180m, alt.ValueNumeric);
        Assert.Empty(alt.PreviousValues);
    }

    [Fact]
    public void A_value_from_another_request_is_not_this_requests_to_correct()
    {
        var request = Resulted();
        var other = Resulted();

        Assert.Throws<InvalidOperationException>(() =>
            request.AmendResult(Value(other, "ALT"), new LabResultInput("ALT", 80m), "ผิด", "stf_lab", Later));
    }
}

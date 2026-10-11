using HappyPaws.Application.Laboratory.CorrectLabResults;
using HappyPaws.Application.Laboratory.Files;
using HappyPaws.Application.Laboratory.ReviewLabResults;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.UnitTests.Application.Laboratory;

/// <summary>
/// What corrections (§ 52), review notes (§ 53) and file requests (§ 54) accept before anything is
/// loaded. See <c>docs/contract-extensions.md</c>.
/// </summary>
public class LabValidationTests
{
    private const string Hex = "0123456789abcdef0123456789abcdef";

    [Theory]
    [InlineData("application/pdf", 1, true)]
    [InlineData("image/jpeg", 10 * 1024 * 1024, true)]
    [InlineData("image/png", 2048, true)]
    [InlineData("image/webp", 2048, true)]
    [InlineData("image/heic", 2048, false)]
    [InlineData("image/svg+xml", 2048, false)]
    [InlineData("text/html", 2048, false)]
    [InlineData(null, 2048, false)]
    [InlineData("application/pdf", 0, false)]
    [InlineData("application/pdf", 10 * 1024 * 1024 + 1, false)]
    public void Only_a_pdf_jpeg_png_or_webp_of_at_most_10_mb_gets_an_upload_url(string? contentType, long sizeBytes, bool valid) =>
        Assert.Equal(valid, new RequestLabFileUploadValidator().Validate(new RequestLabFileUploadCommand("lab_1", contentType, sizeBytes)).IsValid);

    [Theory]
    [InlineData("storage:lab-files/lab_1/file-" + Hex + ".pdf", "ผล.pdf", true)]
    [InlineData("storage:lab-files/lab_1/file-" + Hex + ".pdf", "", false)]
    [InlineData("lab-files/lab_1/file-" + Hex + ".pdf", "ผล.pdf", false)]
    [InlineData("storage:clinic-brand/logo-" + Hex + ".png", "logo.png", false)]
    [InlineData(null, "ผล.pdf", false)]
    public void Attaching_takes_only_a_value_the_upload_made_and_a_name(string? file, string fileName, bool valid) =>
        Assert.Equal(valid, new AttachLabFileValidator().Validate(new AttachLabFileCommand("lab_1", file, fileName)).IsValid);

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("พิมพ์ผิด", true)]
    public void A_correction_and_a_void_need_a_reason(string? reason, bool valid)
    {
        Assert.Equal(valid, new AmendLabResultValidator().Validate(
            new AmendLabResultCommand("lab_1", "lbr_1", new LabResultInput("ALT", 80m), reason)).IsValid);
        Assert.Equal(valid, new VoidLabResultValidator().Validate(new VoidLabResultCommand("lab_1", "lbr_1", reason)).IsValid);
    }

    [Fact]
    public void A_reason_is_at_most_500_characters()
    {
        var reason = new string('ก', LabResult.MaxReasonLength + 1);

        Assert.False(new AmendLabResultValidator().Validate(
            new AmendLabResultCommand("lab_1", "lbr_1", new LabResultInput("ALT", 80m), reason)).IsValid);
        Assert.False(new VoidLabResultValidator().Validate(new VoidLabResultCommand("lab_1", "lbr_1", reason)).IsValid);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("ปกติ", true)]
    [InlineData(1_000, true)]
    [InlineData(1_001, false)]
    public void A_review_note_is_optional_and_at_most_1000_characters(object? note, bool valid)
    {
        var text = note is int length ? new string('ก', length) : (string?)note;

        Assert.Equal(valid, new ReviewLabResultsValidator().Validate(new ReviewLabResultsCommand("lab_1", text)).IsValid);
    }
}

using HappyPaws.Domain.Common;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.UnitTests.Domain.Laboratory;

/// <summary>
/// <c>docs/contract-extensions.md § 54</c>: a lab file's key names its request and its type, so a
/// file is attached only to the request it was uploaded for and served only as the type it was.
/// </summary>
public class LabFileTests
{
    private const string Hex = "0123456789abcdef0123456789abcdef";
    private static readonly DateTimeOffset At = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private static LabRequest Request() =>
        LabRequest.Create("pet_1", "brn_1", "stf_vet", At, LabDestination.External, [new LabTest("Histopathology")], externalLabName: "Vet Central Lab");

    [Theory]
    [InlineData("application/pdf", ".pdf")]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public void A_new_key_reads_back_as_a_file_of_that_request_and_type(string contentType, string extension)
    {
        var key = LabFile.NewKey("lab_06GFF0ABCDEF", contentType);
        var value = LabFile.ValueOf(key);

        Assert.StartsWith("storage:lab-files/lab_06GFF0ABCDEF/file-", value, StringComparison.Ordinal);
        Assert.EndsWith(extension, key, StringComparison.Ordinal);
        Assert.Equal(key, LabFile.KeyOf(value));
        Assert.Equal("lab_06GFF0ABCDEF", LabFile.RequestOf(key));
        Assert.Equal(contentType, LabFile.ContentTypeOf(key));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("lab-files/lab_1/file-" + Hex + ".pdf")]
    [InlineData("storage:lab-files/lab_1/file-" + Hex + ".svg")]
    [InlineData("storage:lab-files/lab_1/file-" + Hex + ".html")]
    [InlineData("storage:lab-files/lab_1/../file-" + Hex + ".pdf")]
    [InlineData("storage:staff-photos/stf_1/photo-" + Hex + ".png")]
    [InlineData("storage:lab-files/pet_1/file-" + Hex + ".pdf")]
    public void A_value_the_upload_did_not_make_has_no_key(string? value) => Assert.Null(LabFile.KeyOf(value));

    [Fact]
    public void A_file_is_attached_with_the_type_its_key_was_made_for()
    {
        var request = Request();
        var key = LabFile.NewKey(request.Id, "application/pdf");

        var attachment = request.Attach(key, "scans/ผล.pdf", 2048, "stf_lab", At);

        Assert.Equal("ผล.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.StartsWith("lbf_", attachment.Id, StringComparison.Ordinal);
        Assert.Single(request.Attachments);
        Assert.Equal(LabRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void A_file_made_for_another_request_is_refused()
    {
        var request = Request();

        var error = Assert.Throws<DomainException>(() =>
            request.Attach(LabFile.NewKey("lab_other", "application/pdf"), "x.pdf", 10, "stf_lab", At));

        Assert.Equal("invalid_lab_attachment", error.Code);
    }

    [Fact]
    public void The_same_file_is_attached_once_and_a_request_holds_at_most_twenty()
    {
        var request = Request();
        var key = LabFile.NewKey(request.Id, "image/png");
        request.Attach(key, "a.png", 10, "stf_lab", At);

        Assert.Throws<DomainException>(() => request.Attach(key, "a.png", 10, "stf_lab", At));

        for (var i = 1; i < LabFile.MaxPerRequest; i++)
        {
            request.Attach(LabFile.NewKey(request.Id, "image/png"), $"{i}.png", 10, "stf_lab", At);
        }

        Assert.Throws<DomainException>(() => request.Attach(LabFile.NewKey(request.Id, "image/png"), "21.png", 10, "stf_lab", At));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(LabFile.MaxBytes + 1)]
    public void A_size_out_of_bounds_is_refused(long size)
    {
        var request = Request();

        Assert.Throws<DomainException>(() => request.Attach(LabFile.NewKey(request.Id, "application/pdf"), "x.pdf", size, "stf_lab", At));
    }

    [Fact]
    public void Removing_a_file_answers_it_and_an_unknown_one_answers_nothing()
    {
        var request = Request();
        var attachment = request.Attach(LabFile.NewKey(request.Id, "application/pdf"), "x.pdf", 10, "stf_lab", At);

        Assert.Null(request.RemoveAttachment("lbf_missing", At));
        Assert.Same(attachment, request.RemoveAttachment(attachment.Id, At));
        Assert.Empty(request.Attachments);
    }
}

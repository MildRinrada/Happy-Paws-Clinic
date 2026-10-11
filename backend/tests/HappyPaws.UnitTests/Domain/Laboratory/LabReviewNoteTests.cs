using HappyPaws.Domain.Common;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.UnitTests.Domain.Laboratory;

/// <summary><c>docs/contract-extensions.md § 53</c>: the vet's note belongs to one reading of the results.</summary>
public class LabReviewNoteTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private static LabRequest Resulted()
    {
        var request = LabRequest.Create("pet_1", "brn_1", "stf_vet", At, LabDestination.InHouse, [new LabTest("CBC")]);
        request.RecordResults([new LabResultInput("WBC", 12m)], "stf_lab", At.AddHours(1));
        return request;
    }

    [Fact]
    public void The_note_is_trimmed_and_kept()
    {
        var request = Resulted();

        request.Review("stf_vet", At.AddHours(2), "  ปกติ  ");

        Assert.Equal("ปกติ", request.ReviewNote);
    }

    [Fact]
    public void A_blank_note_is_no_note()
    {
        var request = Resulted();

        request.Review("stf_vet", At.AddHours(2), "   ");

        Assert.Null(request.ReviewNote);
    }

    [Fact]
    public void A_note_over_the_limit_is_refused_and_the_results_stay_unread()
    {
        var request = Resulted();

        Assert.Throws<DomainException>(() => request.Review("stf_vet", At, new string('x', LabRequest.MaxReviewNoteLength + 1)));
        Assert.Equal(LabRequestStatus.Resulted, request.Status);
    }

    [Fact]
    public void New_results_clear_the_note_with_the_reading()
    {
        var request = Resulted();
        request.Review("stf_vet", At.AddHours(2), "ปกติ");

        request.RecordResults([new LabResultInput("BUN", 20m)], "stf_lab", At.AddHours(3));

        Assert.Null(request.ReviewNote);
        Assert.Equal(LabRequestStatus.Resulted, request.Status);
    }
}

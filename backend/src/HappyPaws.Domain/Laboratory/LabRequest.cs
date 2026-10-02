using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Laboratory;

/// <summary>
/// A request for laboratory work — <c>lab_338</c>, <c>POST /staff/lab-requests</c>,
/// "ส่งแล็บในคลินิกหรือแล็บภายนอก".
/// <para>
/// Its own aggregate rather than a part of the consultation. Results arrive days after the case is
/// closed and its record signed, and a signed record cannot change; a lab request that lived inside
/// the case could never receive its own results. The case id is kept as a link instead.
/// </para>
/// <para>
/// The status follows the two halves of the worklist the contract describes, "งานค้างของหมอ · ผลที่
/// ยังไม่มีคนอ่าน": <see cref="LabRequestStatus.Pending"/> is waiting on the lab, and
/// <see cref="LabRequestStatus.Resulted"/> is waiting on a vet.
/// </para>
/// </summary>
public sealed class LabRequest : Entity
{
    public const int MaxTests = 30;
    public const int MaxResultsPerPost = 100;
    public const int MaxResults = 300;

    private readonly List<LabTest> _tests = [];
    private readonly List<LabResult> _results = [];

    private LabRequest()
    {
    }

    public string PetId { get; private set; } = string.Empty;

    public string BranchId { get; private set; } = string.Empty;

    /// <summary>The visit the sample was taken in. Null for a request made outside a consultation.</summary>
    public string? ConsultationId { get; private set; }

    public LabDestination Destination { get; private set; }

    /// <summary>Who the sample went to, when it left the building.</summary>
    public string? ExternalLabName { get; private set; }

    public LabPriority Priority { get; private set; }

    public IReadOnlyList<LabTest> Tests => _tests;

    /// <summary>What the lab should know — fasting, sedated, suspected condition.</summary>
    public string? ClinicalNotes { get; private set; }

    public string RequestedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; private set; }

    public LabRequestStatus Status { get; private set; } = LabRequestStatus.Pending;

    public IReadOnlyList<LabResult> Results => _results;

    /// <summary>When the most recent results arrived.</summary>
    public DateTimeOffset? ResultedAt { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public string? ReviewedByStaffId { get; private set; }

    public int AbnormalCount => _results.Count(result => result.IsAbnormal);

    public static LabRequest Create(
        string petId,
        string branchId,
        string requestedByStaffId,
        DateTimeOffset requestedAt,
        LabDestination destination,
        IReadOnlyCollection<LabTest> tests,
        LabPriority priority = LabPriority.Routine,
        string? externalLabName = null,
        string? consultationId = null,
        string? clinicalNotes = null)
    {
        if (tests.Count is 0 or > MaxTests)
        {
            throw Invalid($"ต้องระบุรายการตรวจ 1 ถึง {MaxTests} รายการ");
        }

        if (tests.Any(test => string.IsNullOrWhiteSpace(test.Name)))
        {
            throw Invalid("รายการตรวจต้องมีชื่อ");
        }

        if (destination == LabDestination.External && string.IsNullOrWhiteSpace(externalLabName))
        {
            throw Invalid("ส่งแล็บภายนอกต้องระบุชื่อแล็บ");
        }

        var request = new LabRequest
        {
            Id = EntityId.New(EntityId.LabRequest),
            PetId = petId,
            BranchId = branchId,
            ConsultationId = consultationId,
            Destination = destination,
            ExternalLabName = destination == LabDestination.External ? externalLabName!.Trim() : null,
            Priority = priority,
            ClinicalNotes = string.IsNullOrWhiteSpace(clinicalNotes) ? null : clinicalNotes.Trim(),
            RequestedByStaffId = requestedByStaffId,
            RequestedAt = requestedAt,
        };

        request._tests.AddRange(tests.Select(test => new LabTest(
            test.Name.Trim(),
            string.IsNullOrWhiteSpace(test.Code) ? null : test.Code.Trim())));

        return request;
    }

    /// <summary>
    /// <c>POST /staff/lab-requests/{id}/results</c> — "ค่าที่วัดได้ + ช่วงอ้างอิง · ธงค่าผิดปกติ".
    /// <para>
    /// May be called more than once. Labs report in parts — the blood count today, the chemistry
    /// tomorrow — and each part is added. New results always put the request back in front of a
    /// vet, even one already read: a reviewed request that quietly gained an abnormal value is the
    /// result nobody reads.
    /// </para>
    /// </summary>
    public IReadOnlyList<LabResult> RecordResults(
        IReadOnlyCollection<LabResultInput> inputs,
        string recordedByStaffId,
        DateTimeOffset recordedAt)
    {
        if (inputs.Count is 0 or > MaxResultsPerPost)
        {
            throw Invalid($"ต้องส่งผล 1 ถึง {MaxResultsPerPost} ค่าต่อครั้ง");
        }

        if (_results.Count + inputs.Count > MaxResults)
        {
            throw Invalid($"คำขอตรวจหนึ่งรายการมีผลได้ไม่เกิน {MaxResults} ค่า");
        }

        var added = inputs.Select(input => LabResult.Record(Id, input, recordedByStaffId, recordedAt)).ToList();

        _results.AddRange(added);
        Status = LabRequestStatus.Resulted;
        ResultedAt = recordedAt;
        ReviewedAt = null;
        ReviewedByStaffId = null;

        return added;
    }

    /// <summary>
    /// A vet has read the results, which takes the request off "ผลที่ยังไม่มีคนอ่าน". Reading it
    /// twice is not an error; reading results that do not exist yet is.
    /// </summary>
    public void Review(string reviewedByStaffId, DateTimeOffset at)
    {
        if (Status == LabRequestStatus.Reviewed)
        {
            return;
        }

        if (Status != LabRequestStatus.Resulted)
        {
            throw new StateConflictException(nameof(LabRequest), Status.ToString(), nameof(Review));
        }

        Status = LabRequestStatus.Reviewed;
        ReviewedAt = at;
        ReviewedByStaffId = reviewedByStaffId;
    }

    private static DomainException Invalid(string message) => new("invalid_lab_request", message);
}

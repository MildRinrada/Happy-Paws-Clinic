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

    /// <summary>The longest note a vet may leave when reading the results (§ 53).</summary>
    public const int MaxReviewNoteLength = 1_000;

    private readonly List<LabTest> _tests = [];
    private readonly List<LabResult> _results = [];
    private readonly List<LabAttachment> _attachments = [];

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

    /// <summary>
    /// What the vet wrote when reading the results — "ค่าไตสูงเล็กน้อย นัดตรวจซ้ำ 2 สัปดาห์"
    /// (<c>docs/contract-extensions.md § 53</c>). Belongs to that reading: results that put the
    /// request back in front of a vet clear it with the rest of the review.
    /// </summary>
    public string? ReviewNote { get; private set; }

    /// <summary>Files from the lab, oldest first (<c>docs/contract-extensions.md § 54</c>).</summary>
    public IReadOnlyList<LabAttachment> Attachments => _attachments;

    /// <summary>Values outside their range, not counting voided ones.</summary>
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
        ClearReview();

        return added;
    }

    /// <summary>
    /// A vet has read the results, which takes the request off "ผลที่ยังไม่มีคนอ่าน". Reading it
    /// twice is not an error, and the second reading changes nothing, its note included; reading
    /// results that do not exist yet is an error. The note is optional (§ 53).
    /// </summary>
    public void Review(string reviewedByStaffId, DateTimeOffset at, string? note = null)
    {
        if (Status == LabRequestStatus.Reviewed)
        {
            return;
        }

        if (Status != LabRequestStatus.Resulted)
        {
            throw new StateConflictException(nameof(LabRequest), Status.ToString(), nameof(Review));
        }

        var cleanNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (cleanNote is { Length: > MaxReviewNoteLength })
        {
            throw Invalid($"บันทึกของสัตวแพทย์ยาวได้ไม่เกิน {MaxReviewNoteLength} ตัวอักษร");
        }

        Status = LabRequestStatus.Reviewed;
        ReviewedAt = at;
        ReviewedByStaffId = reviewedByStaffId;
        ReviewNote = cleanNote;
    }

    /// <summary>
    /// Corrects one value (<c>docs/contract-extensions.md § 52</c>). The value it replaces stays on
    /// the result. Results a vet has already read go back in front of a vet, the way new results
    /// do: the value they read is no longer the value on file.
    /// </summary>
    /// <returns>The value as it was before, for the audit trail.</returns>
    public LabResultRevision AmendResult(
        LabResult result,
        LabResultInput corrected,
        string reason,
        string staffId,
        DateTimeOffset at)
    {
        EnsureOwn(result);
        result.Amend(corrected, reason, staffId, at);
        ReopenAfterCorrection(at);

        return result.PreviousValues[^1];
    }

    /// <summary>
    /// Strikes one value out (§ 52). It stays on the request, marked, and counts towards nothing.
    /// Reopens read results the same way a correction does.
    /// </summary>
    public void VoidResult(LabResult result, string reason, string staffId, DateTimeOffset at)
    {
        EnsureOwn(result);
        result.Void(reason, staffId, at);
        ReopenAfterCorrection(at);
    }

    /// <summary>
    /// Attaches a file that has already been uploaded under a key made for this request (§ 54).
    /// A file does not change whether the results have been read: it is the paper behind values
    /// that are entered, and read, as values.
    /// </summary>
    public LabAttachment Attach(string storageKey, string fileName, long? sizeBytes, string staffId, DateTimeOffset at)
    {
        if (_attachments.Count >= LabFile.MaxPerRequest)
        {
            throw LabAttachment.Invalid($"คำขอตรวจหนึ่งรายการแนบไฟล์ได้ไม่เกิน {LabFile.MaxPerRequest} ไฟล์");
        }

        if (_attachments.Any(attachment => attachment.StorageKey == storageKey))
        {
            throw LabAttachment.Invalid("แนบไฟล์นี้ไว้แล้ว");
        }

        var attachment = LabAttachment.Create(Id, storageKey, fileName, sizeBytes, staffId, at);
        _attachments.Add(attachment);
        UpdatedAt = at;

        return attachment;
    }

    /// <summary>Takes a file off the request. Null when the request has no such file.</summary>
    public LabAttachment? RemoveAttachment(string attachmentId, DateTimeOffset at)
    {
        var attachment = _attachments.FirstOrDefault(item => item.Id == attachmentId);
        if (attachment is null)
        {
            return null;
        }

        _attachments.Remove(attachment);
        UpdatedAt = at;

        return attachment;
    }

    private void EnsureOwn(LabResult result)
    {
        if (!_results.Contains(result))
        {
            throw new InvalidOperationException($"Result {result.Id} is not on lab request {Id}.");
        }
    }

    /// <summary>
    /// A correction changes the row of the result, not of the request; touching the request is
    /// what moves its <c>version</c>, so a stale <c>If-Match</c> on the next correction conflicts.
    /// </summary>
    private void ReopenAfterCorrection(DateTimeOffset at)
    {
        UpdatedAt = at;

        if (Status == LabRequestStatus.Reviewed)
        {
            Status = LabRequestStatus.Resulted;
            ResultedAt = at;
            ClearReview();
        }
    }

    private void ClearReview()
    {
        ReviewedAt = null;
        ReviewedByStaffId = null;
        ReviewNote = null;
    }

    private static DomainException Invalid(string message) => new("invalid_lab_request", message);
}

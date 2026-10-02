namespace HappyPaws.Domain.Laboratory;

/// <summary>
/// Where a lab request is. The contract names <c>pending</c> (<c>?status=pending</c>) and describes
/// the other two: results that arrived, and results "ที่ยังไม่มีคนอ่าน" until someone has.
/// </summary>
public enum LabRequestStatus
{
    /// <summary>Sent, no results yet.</summary>
    Pending,

    /// <summary>Results are in and nobody has read them.</summary>
    Resulted,

    /// <summary>A vet has read the results.</summary>
    Reviewed,
}

/// <summary>"ส่งแล็บในคลินิกหรือแล็บภายนอก".</summary>
public enum LabDestination
{
    InHouse,
    External,
}

/// <summary>How soon the result is needed. Urgent requests sort first on the worklist.</summary>
public enum LabPriority
{
    Routine,
    Urgent,
}

/// <summary>"ธงค่าผิดปกติ" — how one measured value compares with its reference range.</summary>
public enum LabFlag
{
    Normal,
    Low,
    High,

    /// <summary>Outside normal with no direction, for a value such as "positive".</summary>
    Abnormal,
}

/// <summary>One test asked for — <c>CBC</c>, <c>BUN</c>, "ตรวจพยาธิในเลือด".</summary>
/// <param name="Name">What the request form says.</param>
/// <param name="Code">The clinic's or the lab's own code, when it has one.</param>
public sealed record LabTest(string Name, string? Code = null);

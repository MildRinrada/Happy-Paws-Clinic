using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Clinics;

/// <summary>
/// A physical site of this clinic instance — <c>brn_sukhumvit39</c>.
/// <para>
/// The contract has no <c>clinic_id</c> anywhere ("ไม่มี clinic_id ใน path ให้สลับได้"), so a branch
/// is the only location dimension in the system. Queues reset per branch, staff are assigned to
/// branches, and availability is computed per branch.
/// </para>
/// <para>
/// Owned by the Clinic-setup module — <c>GET /clinic/branches</c> reads it and
/// <c>POST /clinic/branches</c> opens a new one. It was defined before that module existed because
/// booking cannot be modelled without it: every appointment happens somewhere.
/// </para>
/// </summary>
public sealed class Branch : Entity
{
    private readonly List<string> _examRooms = [];

    private Branch()
    {
    }

    /// <summary>Short human code used on printed queue tickets and reports.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Street address as the clinic prints it. Null until someone fills it in.</summary>
    public string? Address { get; private set; }

    /// <summary>Map position ("พิกัด"). Both halves are set or neither is.</summary>
    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// The consulting rooms at this site — <c>room_2</c>. The contract puts them here:
    /// <c>GET /clinic/branches</c> returns "สาขา ที่อยู่ พิกัด <b>ห้องตรวจ</b>".
    /// <para>
    /// Having a registry at all is what lets <c>POST /staff/queue/{id}/assign</c> reject a room
    /// that does not exist, instead of accepting any string and producing a queue board that sends
    /// owners to a door that is not there.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> ExamRooms => _examRooms;

    public static Branch Create(
        string code,
        string name,
        string? id = null,
        IEnumerable<string>? examRooms = null,
        string? address = null,
        double? latitude = null,
        double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("invalid_branch", "รหัสสาขาต้องไม่ว่าง");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("invalid_branch", "ชื่อสาขาต้องไม่ว่าง");
        }

        var branch = new Branch
        {
            // Callers may supply a readable id (brn_sukhumvit39, as the contract shows) instead of
            // a generated one; branches are created by hand at setup, not by the thousand.
            Id = id ?? EntityId.New(EntityId.Branch),
            Code = code.Trim(),
            Name = name.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
        };

        branch.SetCoordinates(latitude, longitude);

        if (examRooms is not null)
        {
            branch.ReplaceExamRooms(examRooms);
        }

        return branch;
    }

    /// <summary>True when the room exists here. An empty registry accepts nothing, not everything.</summary>
    public bool HasExamRoom(string roomId) => _examRooms.Contains(roomId);

    public void ReplaceExamRooms(IEnumerable<string> examRooms)
    {
        var rooms = examRooms
            .Where(room => !string.IsNullOrWhiteSpace(room))
            .Select(room => room.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        _examRooms.Clear();
        _examRooms.AddRange(rooms);
    }

    public void Deactivate() => IsActive = false;

    private void SetCoordinates(double? latitude, double? longitude)
    {
        if ((latitude is null) != (longitude is null))
        {
            // Half a coordinate is a pin in the ocean. Refuse it rather than store it.
            throw new DomainException("invalid_branch", "พิกัดต้องระบุทั้งละติจูดและลองจิจูด");
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new DomainException("invalid_branch", "พิกัดอยู่นอกช่วงที่เป็นไปได้");
        }

        Latitude = latitude;
        Longitude = longitude;
    }
}

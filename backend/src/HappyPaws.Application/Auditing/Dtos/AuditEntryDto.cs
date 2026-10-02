using System.Text.Json;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Auditing.Dtos;

/// <summary>
/// An audit row as the API returns it — "ใคร ทำอะไร กับเรกคอร์ดไหน เมื่อไหร่ จาก IP ใด".
/// <para>
/// <see cref="Details"/> is re-parsed from the stored JSON rather than returned as a string, so a
/// client renders a field-level diff as structure instead of as an escaped blob.
/// </para>
/// </summary>
public sealed record AuditEntryDto(
    string Id,
    string Action,
    string ResourceType,
    string ResourceId,
    DateTimeOffset OccurredAt,
    string? ActorUserId,
    string? ActorStaffId,
    string? ActorCustomerId,
    string? PetId,
    string? BranchId,
    string? IpAddress,
    string? RequestId,
    JsonElement? Details)
{
    public static AuditEntryDto From(AuditEntry entry) =>
        new(
            entry.Id,
            entry.Action,
            entry.ResourceType,
            entry.ResourceId,
            entry.OccurredAt,
            entry.ActorUserId,
            entry.ActorStaffId,
            entry.ActorCustomerId,
            entry.PetId,
            entry.BranchId,
            entry.IpAddress,
            entry.RequestId,
            Parse(entry.DetailsJson));

    private static JsonElement? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(json).RootElement.Clone();
        }
        catch (JsonException)
        {
            // A row written by an older shape, or hand-edited. The log is evidence: show that
            // something is there rather than failing the whole page.
            return null;
        }
    }
}

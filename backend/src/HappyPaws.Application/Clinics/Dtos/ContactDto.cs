using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.Dtos;

/// <summary>One contact entry as <c>GET /clinic/contacts</c> returns it. Not in the API reference.</summary>
public sealed record ContactDto(
    string Id,
    uint Version,
    string Kind,
    string Label,
    string Value,
    string? Url,
    string? EmbedUrl,
    string? BranchId,
    int SortOrder)
{
    public static ContactDto From(ClinicContact contact) =>
        new(
            contact.Id,
            contact.Version,
            contact.Kind,
            contact.Label,
            contact.Value,
            contact.Url,
            contact.EmbedUrl,
            contact.BranchId,
            contact.SortOrder);
}

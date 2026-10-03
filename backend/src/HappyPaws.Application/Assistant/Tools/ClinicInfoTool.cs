using System.Text.Json;
using HappyPaws.Application.Clinics.GetClinicProfile;
using HappyPaws.Application.Clinics.GetOpeningHours;
using HappyPaws.Application.Clinics.ListBranches;
using HappyPaws.Application.Clinics.ManageContacts;

namespace HappyPaws.Application.Assistant.Tools;

/// <summary>
/// <c>get_clinic_info</c> — the clinic's name, branches, contact entries, opening hours and coming
/// closures: what the contact page shows, read through the same use cases.
/// </summary>
public sealed class ClinicInfoTool(
    GetClinicProfileHandler profile,
    ListBranchesHandler branches,
    ListContactsHandler contacts,
    GetOpeningHoursHandler hours) : IAssistantTool
{
    public string Name => "get_clinic_info";

    public string Description =>
        "Returns the clinic's name, branches with addresses, phone numbers, emergency hotline, email, LINE and other "
        + "contact channels, map links, weekly opening hours and upcoming holidays. Use it for any question about "
        + "how to contact the clinic, where it is, or when it is open.";

    public string ParametersSchema => """{"type":"object","properties":{}}""";

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var clinic = await profile.HandleAsync(cancellationToken);
        var sites = await branches.HandleAsync(cancellationToken);
        var entries = await contacts.HandleAsync(cancellationToken);
        var week = await hours.HandleAsync(cancellationToken);

        string? BranchName(string? id) => id is null ? null : sites.FirstOrDefault(site => site.Id == id)?.Name;

        return new ToolResult(
            new
            {
                clinic_name = clinic.Name,
                branches = sites.Select(site => new { id = site.Id, name = site.Name, address = site.Address }),
                contacts = entries.Select(entry => new
                {
                    kind = entry.Kind,
                    label = entry.Label,
                    value = entry.Value,
                    link = entry.Url,
                    branch = BranchName(entry.BranchId) ?? "ทั้งคลินิก",
                }),
                opening_hours = week.Configured
                    ? week.Days.Select(day => new { day = day.Day, opens = day.Opens, closes = day.Closes, closed = day.Closed })
                    : null,
                upcoming_holidays = week.Holidays.Select(day => new
                {
                    date = day.Date.ToString("yyyy-MM-dd"),
                    name = day.Name,
                    branch = BranchName(day.BranchId) ?? "ทุกสาขา",
                }),
            },
            [new AssistantAction("ดูช่องทางติดต่อ", "/contact")]);
    }
}

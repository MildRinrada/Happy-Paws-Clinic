using System.Globalization;
using System.Text.Json;
using HappyPaws.Application.Appointments.GetAvailability;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Assistant.Tools;

/// <summary>
/// <c>find_available_times</c> — the free start times for a service on a day, from the same feed the
/// booking page reads. It never books: the visitor books on the booking page, signed in, and the
/// reply carries the button that takes them there.
/// </summary>
public sealed class AvailabilityTool(GetAvailabilityHandler availability) : IAssistantTool
{
    /// <summary>Enough to answer "is there anything this afternoon"; the booking page has the rest.</summary>
    private const int MaxTimes = 16;

    public string Name => "find_available_times";

    public string Description =>
        "Finds free appointment start times for one service on one day (clinic time, Asia/Bangkok). Needs a "
        + "service_id from list_services and a date as YYYY-MM-DD. It does not book; tell the visitor to book on "
        + "the booking page.";

    public string ParametersSchema =>
        """
        {"type":"object","properties":{
          "service_id":{"type":"string","description":"The service's id from list_services."},
          "date":{"type":"string","description":"The day, YYYY-MM-DD."},
          "branch_id":{"type":"string","description":"Branch id from get_clinic_info. Required when the clinic has more than one branch; ask the visitor which one."}
        },"required":["service_id","date"]}
        """;

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var serviceId = ToolArguments.String(arguments, "service_id");
        var date = ToolArguments.String(arguments, "date");

        if (serviceId is null || !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return new ToolResult(new { error = "ต้องระบุ service_id และ date รูปแบบ YYYY-MM-DD" });
        }

        try
        {
            var found = await availability.HandleAsync(
                new GetAvailabilityQuery(serviceId, day, BranchId: ToolArguments.String(arguments, "branch_id")),
                cancellationToken);

            var times = found.Slots
                .Select(slot => slot.StartsAt.ToOffset(TimeSpan.FromHours(7)).ToString("HH:mm", CultureInfo.InvariantCulture))
                .Distinct()
                .ToList();

            return new ToolResult(
                new { date = found.Date.ToString("yyyy-MM-dd"), duration_minutes = found.DurationMinutes, free_times = times.Take(MaxTimes), more = times.Count > MaxTimes },
                [new AssistantAction("จองนัดออนไลน์", "/portal/book")]);
        }
        catch (AppException refused)
        {
            // A wrong id or a closed day is an answer the model can pass on, not a fault.
            return new ToolResult(new { error = refused.Message });
        }
    }
}

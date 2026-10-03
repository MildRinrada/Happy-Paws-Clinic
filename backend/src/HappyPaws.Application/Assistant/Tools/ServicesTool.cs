using System.Text.Json;
using HappyPaws.Application.ServiceCatalog.ListBookableServices;

namespace HappyPaws.Application.Assistant.Tools;

/// <summary>
/// <c>list_services</c> — the public service menu with today's prices: the services the clinic has
/// opened for online booking, and nothing else.
/// </summary>
public sealed class ServicesTool(ListBookableServicesHandler services) : IAssistantTool
{
    public string Name => "list_services";

    public string Description =>
        "Lists the services that can be booked online, with their id, category, length in minutes and starting "
        + "price in baht. Use it for questions about services or prices, and to find a service_id before calling "
        + "find_available_times.";

    public string ParametersSchema =>
        """{"type":"object","properties":{"branch_id":{"type":"string","description":"Only services offered at this branch. Omit for all branches."}}}""";

    public async Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var branch = ToolArguments.String(arguments, "branch_id");
        var menu = await services.HandleAsync(new ListBookableServicesQuery(BranchId: branch), cancellationToken);

        return new ToolResult(new
        {
            services = menu.Select(service => new
            {
                id = service.Id,
                name = service.Name,
                category = service.CategoryCode,
                duration_minutes = service.DurationMinutes,
                price_from_baht = service.PriceSatang / 100m,
            }),
            note = "ราคาเริ่มต้น ราคาจริงขึ้นกับน้ำหนักและอาการของสัตว์",
        });
    }
}

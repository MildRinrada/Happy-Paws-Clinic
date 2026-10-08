using HappyPaws.Application.Inventory.AdjustStock;
using HappyPaws.Application.Inventory.CreatePurchaseOrder;
using HappyPaws.Application.Inventory.Dispensing;
using HappyPaws.Application.Inventory.ListAlerts;
using HappyPaws.Application.Inventory.ListItems;
using HappyPaws.Application.Inventory.RecordMovement;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Inventory;

/// <summary>
/// Registers the stock of drugs and supplies: the five endpoints, and the
/// <see cref="StockDispenser"/> the pharmacy's dispense calls to take stock off the shelf.
/// </summary>
public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<InventoryBranches>();
        services.AddScoped<InventoryItemResolver>();
        services.AddScoped<StockView>();
        services.AddScoped<StockDispenser>();

        services.AddScoped<ListItemsHandler>();
        services.AddScoped<RecordMovementHandler>();
        services.AddScoped<AdjustStockHandler>();
        services.AddScoped<ListAlertsHandler>();
        services.AddScoped<CreatePurchaseOrderHandler>();

        return services;
    }
}

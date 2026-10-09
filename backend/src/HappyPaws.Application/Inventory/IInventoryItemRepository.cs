using HappyPaws.Domain.Inventory;

namespace HappyPaws.Application.Inventory;

/// <summary>Persistence port for the catalogue of drugs and supplies.</summary>
public interface IInventoryItemRepository
{
    void Add(InventoryItem item);

    Task<InventoryItem?> FindAsync(string itemId, CancellationToken cancellationToken = default);

    /// <summary>The item whose <see cref="InventoryItem.NameKey"/> is this key — how a prescription is matched.</summary>
    Task<InventoryItem?> FindByNameKeyAsync(string nameKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItem>> FindManyAsync(
        IReadOnlyCollection<string> itemIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The catalogue by name, narrowed to names containing <paramref name="search"/> when given, and
    /// to items in use (<paramref name="retired"/> false) or retired ones (true) when given.
    /// </summary>
    Task<IReadOnlyList<InventoryItem>> ListAsync(
        string? search,
        bool? retired = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Medicines in use only (never a retired item, <c>docs/contract-extensions.md § 25</c>), by name,
    /// narrowed to names containing <paramref name="search"/> when given, at most
    /// <paramref name="limit"/> of them — the prescriber's drug picker (§ 20).
    /// </summary>
    Task<IReadOnlyList<InventoryItem>> SearchMedicinesAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken = default);
}

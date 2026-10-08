import type { BranchStock, InventoryItem } from '../types/inventory.types';

/** The item's stock at one branch, or an empty shelf when the answer did not include it. */
export function stockAt(item: InventoryItem, branchId: string | undefined): BranchStock {
  return (
    item.branches.find((branch) => branch.branch_id === branchId) ??
    item.branches[0] ?? {
      branch_id: branchId ?? '',
      on_hand: 0,
      available: 0,
      status: 'out',
      on_order: 0,
      lots: [],
      open_orders: [],
    }
  );
}

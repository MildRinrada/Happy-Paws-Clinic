import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { InventoryItem, ItemChanges, PurchaseOrder, PurchaseOrderStatus } from '../types/inventory.types';

/**
 * `PATCH /staff/inventory/items/{id}`. Needs `inventory:write`. Only the fields sent change;
 * `active: false` retires the item. A price applies to dispenses from now on. The version read goes
 * back as `If-Match`. docs/contract-extensions.md § 25.
 */
export function updateItem(id: string, changes: ItemChanges, version?: number): Promise<InventoryItem> {
  return staffApi().patch<InventoryItem>(`/staff/inventory/items/${encodeURIComponent(id)}`, { body: changes, ifMatch: version });
}

export interface PurchaseOrderQuery {
  status?: PurchaseOrderStatus;
  branch?: string;
  supplier?: string;
  cursor?: string;
}

/** `GET /staff/inventory/purchase-orders`. Needs `inventory:purchase`. Newest first, with lines. § 25. */
export function listPurchaseOrders(query: PurchaseOrderQuery = {}): Promise<CursorPage<PurchaseOrder>> {
  return staffApi().get<CursorPage<PurchaseOrder>>('/staff/inventory/purchase-orders', {
    query: { status: query.status, branch: query.branch, supplier: query.supplier || undefined, cursor: query.cursor, limit: 20 },
  });
}

/**
 * `POST /staff/inventory/purchase-orders/{id}/cancel`. Needs `inventory:purchase`. Calls off what is
 * outstanding; received lines stay. A fully received order is `409 state_conflict`. § 25.
 */
export function cancelPurchaseOrder(id: string, version: number, reason?: string): Promise<PurchaseOrder> {
  return staffApi().post<PurchaseOrder>(`/staff/inventory/purchase-orders/${encodeURIComponent(id)}/cancel`, {
    body: { reason },
    ifMatch: version,
  });
}

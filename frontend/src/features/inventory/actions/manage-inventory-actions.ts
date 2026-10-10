'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { cancelPurchaseOrder, updateItem } from '../api/manageInventory';
import { inventoryPaths } from '../lib/inventory-paths';
import type { InventoryItem, PurchaseOrder } from '../types/inventory.types';

/**
 * Editing and retiring items, and cancelling orders (docs/contract-extensions.md § 25). The backend
 * decides whether a name is free and whether an order still has anything to cancel.
 */

const id = z.string().trim().min(1).max(40);
const version = z.number().int().nonnegative();

const itemChanges = z.object({
  id,
  version,
  changes: z.object({
    name: z.string().trim().min(1).max(200).optional(),
    unit: z.string().trim().min(1).max(40).optional(),
    unit_price_satang: z.number().int().nonnegative().optional(),
    reorder_level: z.number().int().nonnegative().optional(),
    active: z.boolean().optional(),
  }),
});

export async function updateItemAction(values: z.infer<typeof itemChanges>): Promise<ActionResult<InventoryItem>> {
  const input = itemChanges.parse(values);
  const result = await runAction(() => updateItem(input.id, input.changes, input.version));

  revalidatePath(inventoryPaths.list, 'layout');
  return result;
}

export async function cancelPurchaseOrderAction(values: { id: string; version: number; reason?: string }): Promise<ActionResult<PurchaseOrder>> {
  const input = z.object({ id, version, reason: z.string().trim().max(1000).optional() }).parse(values);
  const result = await runAction(() => cancelPurchaseOrder(input.id, input.version, input.reason || undefined));

  revalidatePath(inventoryPaths.list, 'layout');
  return result;
}

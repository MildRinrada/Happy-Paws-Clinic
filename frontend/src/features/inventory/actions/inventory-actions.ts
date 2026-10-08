'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { adjustStock } from '../api/adjustStock';
import { createPurchaseOrder } from '../api/createPurchaseOrder';
import { recordMovement } from '../api/recordMovement';
import { inventoryPaths } from '../lib/inventory-paths';

/**
 * The stock room's three writes. Nothing about stock is decided here: whether there is enough to
 * issue, whether a receipt fits its order, what a count changes, is the backend's to answer under
 * its stock lock. These check the shape, make the one call, and mark the stock pages stale.
 */

const id = z.string().trim().min(1).max(40);
const optionalText = (max: number) => z.string().trim().max(max).optional();
const day = z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional();
const satang = z.number().int().nonnegative();

const newItem = z.object({
  name: z.string().trim().min(1).max(200),
  kind: z.enum(['medicine', 'supply']),
  unit: z.string().trim().min(1).max(40),
  unit_price_satang: satang,
  reorder_level: z.number().int().nonnegative(),
});

function revalidate() {
  revalidatePath(inventoryPaths.list, 'layout');
}

const movement = z.object({
  idempotencyKey: z.uuid(),
  input: z.object({
    type: z.enum(['receive', 'issue', 'transfer']),
    branch_id: id.optional(),
    to_branch_id: id.optional(),
    item_id: id.optional(),
    new_item: newItem.optional(),
    quantity: z.number().int().positive(),
    lot_id: id.optional(),
    lot_number: optionalText(60),
    expires_on: day,
    unit_cost_satang: satang.optional(),
    purchase_order_id: id.optional(),
    reference: optionalText(120),
    note: optionalText(500),
  }),
});

export async function recordMovementAction(values: z.infer<typeof movement>) {
  const { idempotencyKey, input } = movement.parse(values);
  const result = await runAction(() => recordMovement(input, idempotencyKey));

  revalidate();
  return result;
}

const adjustment = z.object({
  branch_id: id.optional(),
  item_id: id,
  lot_id: id.optional(),
  counted_quantity: z.number().int().nonnegative(),
  reason: z.string().trim().min(1).max(500),
});

export async function adjustStockAction(values: z.infer<typeof adjustment>) {
  const input = adjustment.parse(values);
  const result = await runAction(() => adjustStock(input));

  revalidate();
  return result;
}

const order = z.object({
  idempotencyKey: z.uuid(),
  input: z.object({
    branch_id: id.optional(),
    supplier_name: z.string().trim().min(1).max(200),
    expected_on: day,
    note: optionalText(1000),
    lines: z
      .array(
        z.object({
          item_id: id.optional(),
          new_item: newItem.optional(),
          quantity: z.number().int().positive(),
          unit_cost_satang: satang,
        }),
      )
      .min(1)
      .max(50),
  }),
});

export async function createPurchaseOrderAction(values: z.infer<typeof order>) {
  const { idempotencyKey, input } = order.parse(values);
  const result = await runAction(() => createPurchaseOrder(input, idempotencyKey));

  revalidate();
  return result;
}

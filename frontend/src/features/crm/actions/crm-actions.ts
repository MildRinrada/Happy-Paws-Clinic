'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { createCoupon } from '../api/createCoupon';
import { createSegment } from '../api/createSegment';
import { sellPackage } from '../api/sellPackage';
import type { Coupon, PackageSale, Segment } from '../types/crm.types';

/**
 * The three CRM writes, all on `crm:manage`. Whether a code is free, whether a segment's numbers
 * add up, what a package covers: the backend decides and refuses with a reason. These check the
 * shape, make the one call, and mark the pages stale.
 */

const id = z.string().trim().min(1).max(40);
const day = z.string().regex(/^\d{4}-\d{2}-\d{2}$/);

const sale = z.object({
  idempotencyKey: z.uuid(),
  input: z.object({
    customer_id: id,
    pet_id: id.nullable().optional(),
    name: z.string().trim().min(1).max(200),
    service_ids: z.array(id).min(1).max(20),
    total_uses: z.number().int().min(1).max(1000),
    price_satang: z.number().int().nonnegative(),
    expires_on: day.nullable().optional(),
    branch_id: id.nullable().optional(),
    note: z.string().trim().max(1000).nullable().optional(),
  }),
});

export async function sellPackageAction(values: z.infer<typeof sale>): Promise<ActionResult<PackageSale>> {
  const { idempotencyKey, input } = sale.parse(values);
  const result = await runAction(() => sellPackage(input, idempotencyKey));

  if (result.ok) {
    revalidatePath('/billing');
    revalidatePath(`/customers/${input.customer_id}`);
  }

  return result;
}

const coupon = z
  .object({
    code: z.string().trim().regex(/^[A-Za-z0-9-]{4,20}$/).optional(),
    name: z.string().trim().min(1).max(120),
    percent: z.number().positive().max(100).optional(),
    amount_satang: z.number().int().positive().optional(),
    service_ids: z.array(id).max(50).optional(),
    max_redemptions: z.number().int().min(1).max(100_000).nullable().optional(),
    per_customer_limit: z.number().int().min(1).max(100).nullable().optional(),
    valid_from: day.optional(),
    valid_until: day.nullable().optional(),
    branch_id: id.nullable().optional(),
  })
  // The API's own rule, restated so a malformed call never leaves the server: one or the other.
  .refine((value) => (value.percent === undefined) !== (value.amount_satang === undefined));

export async function createCouponAction(values: z.infer<typeof coupon>): Promise<ActionResult<Coupon>> {
  const input = coupon.parse(values);
  const result = await runAction(() => createCoupon(input));

  if (result.ok) revalidatePath('/crm/coupons');
  return result;
}

const days = z.number().int().min(1).max(3650);

const segment = z.object({
  name: z.string().trim().min(1).max(120),
  criteria: z.object({
    species: z.array(z.string().trim().min(1).max(60)).max(10).optional(),
    inactive_days: days.optional(),
    visited_within_days: days.optional(),
    min_spend_satang: z.number().int().positive().optional(),
    tiers: z.array(z.enum(['member', 'silver', 'gold'])).optional(),
    has_active_package: z.boolean().optional(),
    branch_id: id.nullable().optional(),
  }),
});

export async function createSegmentAction(values: z.infer<typeof segment>): Promise<ActionResult<Segment>> {
  const input = segment.parse(values);
  const result = await runAction(() => createSegment(input));

  if (result.ok) revalidatePath('/crm/segments');
  return result;
}

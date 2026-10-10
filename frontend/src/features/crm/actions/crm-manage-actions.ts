'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { adjustPoints } from '../api/adjustPoints';
import { deactivateCoupon } from '../api/coupons';
import { replaceLoyaltySettings } from '../api/loyaltySettings';
import type { Coupon, LoyaltySettings, PointsAdjustment } from '../types/crm.types';

/**
 * Managing what the CRM pages created (docs/contract-extensions.md § 25). The backend decides
 * whether a coupon may be switched off, whether points can be taken, and whether the programme makes
 * sense; these check the shape, make the call, and mark the pages stale.
 */

const id = z.string().trim().min(1).max(40);

export async function deactivateCouponAction(values: { id: string; reason?: string }): Promise<ActionResult<Coupon>> {
  const input = z.object({ id, reason: z.string().trim().max(500).optional() }).parse(values);
  const result = await runAction(() => deactivateCoupon(input.id, input.reason || undefined));

  revalidatePath('/crm/coupons');
  return result;
}

const adjustment = z.object({
  idempotencyKey: z.uuid(),
  customerId: id,
  points: z.number().int().min(-100_000).max(100_000).refine((points) => points !== 0),
  reason: z.string().trim().min(1).max(200),
});

export async function adjustPointsAction(values: z.infer<typeof adjustment>): Promise<ActionResult<PointsAdjustment>> {
  const { idempotencyKey, customerId, points, reason } = adjustment.parse(values);
  const result = await runAction(() => adjustPoints(customerId, { points, reason }, idempotencyKey));

  if (result.ok) {
    revalidatePath('/crm/loyalty');
    revalidatePath(`/customers/${customerId}`);
  }

  return result;
}

const whole = z.number().int();

const programme = z.object({
  version: z.number().int().nonnegative(),
  values: z.object({
    satang_per_point: whole.min(100).max(10_000_000),
    point_value_satang: whole.min(1).max(100_000),
    silver_min_points: whole.min(1),
    gold_min_points: whole.min(2),
    points_expire_after_months: whole.min(1).max(120).nullable(),
    // Refer a friend (docs/contract-extensions.md § 39). Optional: one not sent keeps its value.
    referral_enabled: z.boolean().optional(),
    referral_referrer_points: whole.min(0).max(10_000).optional(),
    referral_friend_points: whole.min(0).max(10_000).optional(),
    referral_monthly_cap: whole.min(1).max(100).optional(),
  }),
});

export async function replaceLoyaltySettingsAction(values: z.infer<typeof programme>): Promise<ActionResult<LoyaltySettings>> {
  const { version, values: input } = programme.parse(values);
  const result = await runAction(() => replaceLoyaltySettings(input, version));

  revalidatePath('/settings/loyalty');
  return result;
}

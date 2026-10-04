'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import {
  createService,
  updateService,
  updateServiceAvailability,
  updateServicePricing,
} from '../api/manage-services';

/**
 * The services page's saves. Each marks the page stale, and the public site too: the menu, the
 * prices and the booking page all read the catalogue.
 */

function refresh() {
  revalidatePath('/settings/services');
  revalidatePath('/', 'layout');
}

const id = z.string().min(1).max(64);
const version = z.number().int().nonnegative();
const whole = z.number().int();
const staffIds = z.array(z.string().min(1).max(40)).max(100);
const day = z.string().regex(/^\d{4}-\d{2}-\d{2}$/);
const time = z.string().regex(/^\d{2}:\d{2}$/);

const newService = z.object({
  name: z.string().trim().max(200),
  category_code: z.string().max(20),
  duration_minutes: whole,
  price_satang: whole,
  code: z.string().trim().max(32).optional(),
  online_booking_enabled: z.boolean(),
});

export async function createServiceAction(values: z.infer<typeof newService>) {
  const parsed = newService.parse(values);
  const result = await runAction(() => createService(parsed));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const details = z.object({
  id,
  version,
  values: z.object({
    name: z.string().trim().max(200),
    category_code: z.string().max(20),
    duration_minutes: whole,
    eligible_staff_ids: staffIds,
    is_active: z.boolean(),
  }),
});

export async function updateServiceAction(payload: z.infer<typeof details>) {
  const parsed = details.parse(payload);
  const result = await runAction(() => updateService(parsed.id, parsed.version, parsed.values));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const pricing = z.object({
  id,
  version,
  values: z.object({
    base_price_satang: whole,
    effective_from: day,
    tiers: z
      .array(
        z.object({
          price_satang: whole,
          species: z.string().trim().max(60).optional(),
          min_weight_kg: z.number().optional(),
          max_weight_kg: z.number().optional(),
        }),
      )
      .max(50),
  }),
});

export async function updateServicePricingAction(payload: z.infer<typeof pricing>) {
  const parsed = pricing.parse(payload);
  const result = await runAction(() => updateServicePricing(parsed.id, parsed.version, parsed.values));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const availability = z.object({
  id,
  version,
  values: z.object({
    online_booking_enabled: z.boolean(),
    branch_ids: z.array(z.string().min(1).max(40)).max(50),
    booking_windows: z.array(z.object({ day: z.string().max(10), from: time, to: time })).max(50),
  }),
});

export async function updateServiceAvailabilityAction(payload: z.infer<typeof availability>) {
  const parsed = availability.parse(payload);
  const result = await runAction(() => updateServiceAvailability(parsed.id, parsed.version, parsed.values));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

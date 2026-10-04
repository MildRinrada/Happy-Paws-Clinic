'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { createShifts, recordTimeOff } from '../api/schedules';

/**
 * The roster page's two writes. The roster is what booking offers, so the booking pages are as
 * stale as this one after either.
 */

function refresh() {
  revalidatePath('/schedules');
  revalidatePath('/appointments');
}

const instant = z.string().regex(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\+07:00$/);
const staffId = z.string().min(1).max(40);

const shifts = z.object({
  staff_id: staffId,
  branch_id: z.string().min(1).max(40),
  starts_at: instant,
  ends_at: instant,
  break: z.object({ starts_at: instant, ends_at: instant }).optional(),
  rrule: z.string().max(300).optional(),
});

export async function createShiftsAction(values: z.infer<typeof shifts>) {
  const parsed = shifts.parse(values);
  const result = await runAction(() => createShifts(parsed));

  refresh();
  return result.ok ? { ok: true as const, data: result.data.length } : result;
}

const timeOff = z.object({
  staff_id: staffId,
  starts_at: instant,
  ends_at: instant,
  reason: z.string().trim().max(200).optional(),
});

export async function recordTimeOffAction(values: z.infer<typeof timeOff>) {
  const parsed = timeOff.parse(values);
  const result = await runAction(() => recordTimeOff(parsed));

  refresh();
  return result;
}

'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { createShifts, recordTimeOff, replaceAssignmentRules } from '../api/schedules';

/**
 * The roster page's writes. The roster is what booking offers, so the booking pages are as stale
 * as this one after a shift or leave. The assignment rules are written whole and never read back.
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

const assignmentRules = z.object({
  rules: z
    .array(
      z.object({
        species: z.string().trim().max(40).optional(),
        service_id: z.string().min(1).max(64).optional(),
        staff_ids: z.array(staffId).min(1).max(20),
      }),
    )
    .max(50),
});

/**
 * Replaces every case-assignment rule with the set given; an empty set removes them all. Nothing
 * on any page shows the rules, so only this page is refreshed.
 */
export async function replaceAssignmentRulesAction(values: z.infer<typeof assignmentRules>) {
  const { rules } = assignmentRules.parse(values);
  const result = await runAction(() => replaceAssignmentRules(rules));

  revalidatePath('/schedules');
  return result;
}

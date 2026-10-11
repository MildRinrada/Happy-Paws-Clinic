'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { changeShift, copyWeek, createShifts, recordTimeOff, removeShift, replaceAssignmentRules } from '../api/schedules';

/**
 * The roster page's writes. The roster is what booking offers, so the booking pages are as stale
 * as this one after a shift or leave. The assignment rules are written whole, guarded by the
 * version they were read with.
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

const version = z.number().int().nonnegative();
const shiftId = z.string().min(1).max(40);

const shiftChange = z.object({
  id: shiftId,
  version,
  starts_at: instant,
  ends_at: instant,
});

/** One block at new times (docs/contract-extensions.md § 47). Answers the block and the appointments left outside it. */
export async function changeShiftAction(values: z.infer<typeof shiftChange>) {
  const { id, version: current, ...times } = shiftChange.parse(values);
  const result = await runAction(() => changeShift(id, times, current));

  refresh();
  return result;
}

const shiftRemoval = z.object({ id: shiftId, version });

/** One block off the roster (§ 47). Answers the appointments that were in it. */
export async function removeShiftAction(values: z.infer<typeof shiftRemoval>) {
  const { id, version: current } = shiftRemoval.parse(values);
  const result = await runAction(() => removeShift(id, current));

  refresh();
  return result;
}

const monday = z.string().regex(/^\d{4}-\d{2}-\d{2}$/);

const weekCopy = z.object({
  branch_id: z.string().min(1).max(40),
  from_week: monday,
  to_week: monday,
});

/** A week of the branch onto another (§ 49), all or nothing; a refusal carries every clash in `details`. */
export async function copyWeekAction(values: z.infer<typeof weekCopy>) {
  const parsed = weekCopy.parse(values);
  const result = await runAction(() => copyWeek(parsed));

  refresh();
  return result;
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
  version: version.optional(),
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
 * Replaces every case-assignment rule with the set given; an empty set removes them all. Only this
 * page shows the rules, so only this page is refreshed.
 */
export async function replaceAssignmentRulesAction(values: z.infer<typeof assignmentRules>) {
  const { rules, version: current } = assignmentRules.parse(values);
  const result = await runAction(() => replaceAssignmentRules(rules, current));

  revalidatePath('/schedules');
  return result;
}

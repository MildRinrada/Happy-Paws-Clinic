'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import {
  addToWaitingList,
  createRecurringSeries,
  detectConflicts,
  detectDayConflicts,
  removeFromWaitingList,
} from '../api/advancedBooking';
import type { ConflictReport } from '../types/advanced-booking.types';

/**
 * Checking times, booking a course, and the waiting list. Which dates are free, which vet takes a
 * course and what counts as a valid window are the backend's to decide; these check the shape and
 * make the calls.
 */

const id = z.string().min(1).max(64);
const instant = z.iso.datetime({ offset: true });
const day = z.iso.date();

/** The backend allows 52 visits in a course; a check covers at most that many dates. */
const MAX_CHECKS = 52;

const conflictCheck = z.object({
  starts: z.array(instant).min(1).max(MAX_CHECKS),
  serviceId: id,
  branchId: id,
  vetId: id.optional(),
  petId: id.optional(),
});

/**
 * Asks `GET /staff/appointments/conflicts` about each start, a few at a time. Answers in the order
 * asked; the first refusal ends the check, because the same query would be refused again.
 */
export async function checkConflictsAction(input: z.infer<typeof conflictCheck>) {
  const parsed = conflictCheck.parse(input);

  return runAction(async () => {
    const reports: ConflictReport[] = [];

    for (let at = 0; at < parsed.starts.length; at += 6) {
      const batch = parsed.starts.slice(at, at + 6);
      reports.push(
        ...(await Promise.all(
          batch.map((startsAt) =>
            detectConflicts({
              startsAt,
              serviceId: parsed.serviceId,
              branchId: parsed.branchId,
              vetId: parsed.vetId,
              petId: parsed.petId,
            }),
          ),
        )),
      );
    }

    return reports;
  });
}

const series = z.object({
  petId: id,
  serviceId: id,
  branchId: id,
  firstStartsAt: instant,
  rrule: z.string().min(1).max(300),
  staffId: id.optional(),
  reason: z.string().trim().max(1000).optional(),
  type: z.string().regex(/^[a-z][a-z0-9_]{1,29}$/).optional(),
  idempotencyKey: z.uuid(),
});

export async function createRecurringSeriesAction(input: z.infer<typeof series>) {
  const parsed = series.parse(input);
  const result = await runAction(() =>
    createRecurringSeries(
      {
        pet_id: parsed.petId,
        service_id: parsed.serviceId,
        branch_id: parsed.branchId,
        first_starts_at: parsed.firstStartsAt,
        rrule: parsed.rrule,
        staff_id: parsed.staffId,
        reason: parsed.reason || undefined,
        type: parsed.type,
      },
      parsed.idempotencyKey,
    ),
  );

  revalidatePath('/appointments');
  return result.ok ? { ok: true as const, data: { count: result.data.appointments.length } } : result;
}

const waiting = z.object({
  petId: id,
  serviceId: id,
  earliestOn: day.optional(),
  latestOn: day.optional(),
  branchId: id.optional(),
  staffId: id.optional(),
  note: z.string().trim().max(500).optional(),
});

const WAITING_LIST_PATH = '/appointments/waiting-list';

/** Adds an entry, and marks the list below the form stale so it shows. */
export async function addToWaitingListAction(input: z.infer<typeof waiting>) {
  const parsed = waiting.parse(input);

  const result = await runAction(() =>
    addToWaitingList({
      pet_id: parsed.petId,
      service_id: parsed.serviceId,
      earliest_on: parsed.earliestOn,
      latest_on: parsed.latestOn,
      branch_id: parsed.branchId,
      staff_id: parsed.staffId,
      note: parsed.note || undefined,
    }),
  );

  revalidatePath(WAITING_LIST_PATH);
  return result;
}

/**
 * `DELETE /staff/waiting-list/{id}`. An entry someone else removed first answers `404`; the list is
 * refreshed either way, so the row goes.
 */
export async function removeFromWaitingListAction(input: { id: string }) {
  const parsed = z.object({ id }).parse(input);
  const result = await runAction(() => removeFromWaitingList(parsed.id));

  revalidatePath(WAITING_LIST_PATH);
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const dayCheck = z.object({ date: day, branchId: id });

/** `GET /staff/appointments/conflicts?date=` for the calendar's day. Reports only. */
export async function checkDayConflictsAction(input: z.infer<typeof dayCheck>) {
  const parsed = dayCheck.parse(input);
  return runAction(() => detectDayConflicts(parsed.date, parsed.branchId));
}

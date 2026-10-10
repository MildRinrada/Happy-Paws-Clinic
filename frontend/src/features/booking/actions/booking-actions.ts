'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { bookAppointment } from '../api/bookAppointment';
import { getAvailability } from '../api/getAvailability';
import { holdSlot } from '../api/holdSlot';

/**
 * The booking page's three calls. Whether a time is free, how far ahead the clinic books, and
 * whether a hold is still live are all the backend's to decide. These only check the shape of
 * what the form sent and hand the answer back.
 */

const id = z.string().min(1).max(64);

const slotsInput = z.object({
  serviceId: id,
  branchId: id,
  date: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
});

export async function loadSlotsAction(input: z.infer<typeof slotsInput>) {
  const parsed = slotsInput.parse(input);
  return runAction(() => getAvailability(parsed.serviceId, parsed.date, parsed.branchId));
}

const holdInput = z.object({
  serviceId: id,
  branchId: id,
  startsAt: z.iso.datetime({ offset: true }),
  staffId: id.optional(),
});

export async function holdSlotAction(input: z.infer<typeof holdInput>) {
  return runAction(() => holdSlot(holdInput.parse(input)));
}

const bookInput = z
  .object({
    petId: id.optional(),
    // A pet booked for the first time, from the guest booking page (docs/contract-extensions.md § 27).
    newPet: z.object({ name: z.string().trim().min(1).max(120), species: z.string().trim().min(1).max(60) }).optional(),
    hold: z.object({ id, service_id: id, branch_id: id, staff_id: id, starts_at: z.iso.datetime({ offset: true }) }),
    type: z.string().regex(/^[a-z][a-z0-9_]{1,29}$/),
    reason: z.string().trim().max(1000).optional(),
    idempotencyKey: z.uuid(),
  })
  .refine((input) => Boolean(input.petId) !== Boolean(input.newPet));

export async function bookAction(input: z.infer<typeof bookInput>) {
  const result = await runAction(() => bookAppointment(bookInput.parse(input)));

  // The new appointment shows on the home card and the appointments page.
  revalidatePath('/portal', 'layout');

  return result;
}

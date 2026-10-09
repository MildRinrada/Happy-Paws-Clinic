'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { checkInAppointment } from '../api/checkInAppointment';
import {
  cancelAppointment,
  confirmAppointment,
  createAppointment,
  markNoShow,
  rescheduleAppointment,
} from '../api/staffAppointments';

/**
 * The calendar's changes. Whether a time is free, whether an appointment may still be moved or
 * marked absent, and who may do it are the backend's to decide; these check the shape, make one
 * call, and mark the calendar and the queue stale.
 */

const CALENDAR_PATH = '/appointments';

function refresh() {
  revalidatePath(CALENDAR_PATH);
  revalidatePath('/queue');
}

const id = z.string().min(1).max(64);
const instant = z.iso.datetime({ offset: true });

const newAppointment = z.object({
  petId: id,
  serviceId: id,
  branchId: id,
  startsAt: instant,
  staffId: id.optional(),
  reason: z.string().trim().max(1000).optional(),
  type: z.string().regex(/^[a-z][a-z0-9_]{1,29}$/).optional(),
  idempotencyKey: z.uuid(),
});

export async function createAppointmentAction(input: z.infer<typeof newAppointment>) {
  const parsed = newAppointment.parse(input);
  const result = await runAction(() =>
    createAppointment(
      {
        pet_id: parsed.petId,
        service_id: parsed.serviceId,
        branch_id: parsed.branchId,
        starts_at: parsed.startsAt,
        staff_id: parsed.staffId,
        reason: parsed.reason || undefined,
        type: parsed.type,
      },
      parsed.idempotencyKey,
    ),
  );

  refresh();
  return result;
}

const reschedule = z.object({
  appointmentId: id,
  version: z.number().int().nonnegative(),
  startsAt: instant,
  staffId: id.optional(),
});

export async function rescheduleAppointmentAction(input: z.infer<typeof reschedule>) {
  const parsed = reschedule.parse(input);
  const result = await runAction(() =>
    rescheduleAppointment(parsed.appointmentId, parsed.version, { starts_at: parsed.startsAt, staff_id: parsed.staffId }),
  );

  refresh();
  return result;
}

const cancel = z.object({ appointmentId: id, reason: z.string().trim().max(500).optional() });

export async function cancelAppointmentAction(input: z.infer<typeof cancel>) {
  const parsed = cancel.parse(input);
  const result = await runAction(() => cancelAppointment(parsed.appointmentId, parsed.reason || undefined));

  refresh();
  return result;
}

const single = z.object({ appointmentId: id });

export async function confirmAppointmentAction(input: z.infer<typeof single>) {
  const { appointmentId } = single.parse(input);
  const result = await runAction(() => confirmAppointment(appointmentId));

  refresh();
  return result;
}

export async function markNoShowAction(input: z.infer<typeof single>) {
  const { appointmentId } = single.parse(input);
  const result = await runAction(() => markNoShow(appointmentId));

  refresh();
  return result;
}

const checkIn = z.object({ appointmentId: id, idempotencyKey: z.uuid() });

/** Puts the pet in today's queue and answers with the number it was given. */
export async function checkInAppointmentAction(input: z.infer<typeof checkIn>) {
  const parsed = checkIn.parse(input);
  const result = await runAction(() => checkInAppointment(parsed.appointmentId, parsed.idempotencyKey));

  refresh();
  return result.ok ? { ok: true as const, data: { number: result.data.ticket.number } } : result;
}

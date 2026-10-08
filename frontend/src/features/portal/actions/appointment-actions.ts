'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { cancelAppointment } from '../api/cancelAppointment';
import { rescheduleAppointment } from '../api/rescheduleAppointment';

/**
 * The owner moving or cancelling their own appointment. Whether the time is free, whether the
 * change cut-off has passed, and whether a cancellation counts as late are all the backend's call.
 * These only check the shape of the request and refresh the portal afterwards.
 */

const id = z.string().min(1).max(64);

const rescheduleInput = z.object({
  appointmentId: id,
  version: z.number().int().nonnegative(),
  startsAt: z.iso.datetime({ offset: true }),
  staffId: id.optional(),
});

export async function rescheduleAction(values: z.infer<typeof rescheduleInput>) {
  const result = await runAction(() => rescheduleAppointment(rescheduleInput.parse(values)));

  // The new time shows on the home card and the appointments page.
  revalidatePath('/portal', 'layout');

  return result;
}

const cancelInput = z.object({
  appointmentId: id,
  reason: z.string().trim().max(500).optional(),
});

export async function cancelAction(values: z.infer<typeof cancelInput>) {
  const { appointmentId, reason } = cancelInput.parse(values);
  const result = await runAction(() => cancelAppointment(appointmentId, reason));

  revalidatePath('/portal', 'layout');

  return result.ok ? { ok: true as const, lateCancellation: Boolean(result.data.late_cancellation) } : result;
}

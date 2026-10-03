'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { checkInAppointment } from '@/features/appointments/api/checkInAppointment';
import { runAction } from '@/lib/actions/action-result';

/**
 * The owner checking in from their phone. Whether it is allowed now (the day, the time, the
 * appointment's state) is the backend's call; this only makes the request and refreshes the page.
 */

const input = z.object({
  appointmentId: z.string().min(1).max(64),
  idempotencyKey: z.uuid(),
});

export async function checkInAction(values: z.infer<typeof input>) {
  const parsed = input.parse(values);
  const result = await runAction(() => checkInAppointment(parsed.appointmentId, parsed.idempotencyKey));

  revalidatePath('/portal');

  return result.ok ? { ok: true as const } : result;
}

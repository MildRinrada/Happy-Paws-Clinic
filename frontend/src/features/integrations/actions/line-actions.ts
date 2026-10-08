'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import type { SendResult } from '@/features/notifications/types/notification.types';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { broadcastLine } from '../api/broadcastLine';
import { configureLine } from '../api/lineSettings';
import type { LineSettings } from '../types/line.types';

/** Saving the LINE settings (`integration:manage`) and sending a broadcast (`notify:broadcast`). */

const settingsInput = z.object({
  version: z.number().int().nonnegative(),
  values: z.object({
    channel_id: z.string().trim().max(20).optional(),
    channel_secret: z.string().trim().max(200).optional(),
    channel_access_token: z.string().trim().max(1000).optional(),
    enabled: z.boolean().optional(),
    notification_types: z.record(z.string().max(60), z.boolean()).optional(),
    broadcast_monthly_limit: z.number().int().min(0).max(1_000_000).optional(),
  }),
});

export async function configureLineAction(payload: z.infer<typeof settingsInput>): Promise<ActionResult<LineSettings>> {
  const { version, values } = settingsInput.parse(payload);
  const result = await runAction(() => configureLine(version, values));

  revalidatePath('/settings/line');
  return result;
}

const broadcastInput = z.object({
  customer_ids: z.array(z.string().min(1).max(40)).min(1).max(200),
  message: z.string().trim().min(1).max(1000),
});

export async function broadcastLineAction(values: z.infer<typeof broadcastInput>): Promise<ActionResult<SendResult>> {
  const input = broadcastInput.parse(values);
  const result = await runAction(() => broadcastLine(input));

  revalidatePath('/notifications/log');
  return result;
}

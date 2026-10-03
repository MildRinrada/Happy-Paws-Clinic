'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { markNotificationsRead } from '../api/notifications';

const input = z.object({ ids: z.array(z.string().min(1).max(64)).max(100).optional() });

/** Marks some messages read, or all of them when no ids are given. Refreshes the bell's count too. */
export async function markReadAction(values: z.infer<typeof input>) {
  const { ids } = input.parse(values);
  const result = await runAction(() => markNotificationsRead(ids));

  revalidatePath('/portal', 'layout');

  return result;
}

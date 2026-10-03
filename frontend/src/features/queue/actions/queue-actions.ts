'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { searchPets } from '@/features/pets/api/searchPets';
import type { PetSummary } from '@/features/pets/types/pet.types';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { assignTicket } from '../api/assignTicket';
import { callTicket } from '../api/callTicket';
import { issueWalkInTicket } from '../api/issueWalkInTicket';
import { leaveQueue } from '../api/leaveQueue';
import { skipTicket } from '../api/skipTicket';
import type { QueueTicket } from '../types/queue.types';

/**
 * What the queue screen can do, as server actions.
 *
 * Each one is a thin crossing: parse what the browser sent, call the one backend endpoint, tell
 * Next the board is stale. No rule about the queue lives here — whether a ticket may be called,
 * by whom, in what state — because the backend owns all of them and answers with a reason when it
 * refuses. The parsing exists because a server action is a public endpoint whatever its callers
 * look like, and its arguments arrive from the network.
 */

const QUEUE_PATH = '/queue';

const ticketRef = z.object({ ticketId: z.string().min(1), version: z.number().int().nonnegative() });

async function onTicket(
  input: unknown,
  mutate: (ticketId: string, version: number) => Promise<QueueTicket>,
): Promise<ActionResult<QueueTicket>> {
  const { ticketId, version } = ticketRef.parse(input);
  const result = await runAction(() => mutate(ticketId, version));

  // Revalidated on a refusal too: a 409 means the board on screen was already out of date.
  revalidatePath(QUEUE_PATH);

  return result;
}

export async function callTicketAction(input: { ticketId: string; version: number }) {
  return onTicket(input, callTicket);
}

export async function skipTicketAction(input: { ticketId: string; version: number }) {
  return onTicket(input, skipTicket);
}

export async function leaveQueueAction(input: { ticketId: string; version: number }) {
  return onTicket(input, (ticketId, version) => leaveQueue(ticketId, version));
}

const assignment = ticketRef.extend({
  staffId: z.string().min(1).optional(),
  roomId: z.string().min(1).optional(),
});

export async function assignTicketAction(input: z.infer<typeof assignment>) {
  const parsed = assignment.parse(input);
  const result = await runAction(() => assignTicket(parsed));

  revalidatePath(QUEUE_PATH);

  return result;
}

const walkIn = z.object({
  petId: z.string().min(1),
  serviceId: z.string().min(1),
  branchId: z.string().min(1),
  priority: z.enum(['normal', 'urgent', 'emergency']),
  priorityReason: z.string().trim().max(300).optional(),
  idempotencyKey: z.uuid(),
});

export async function issueWalkInTicketAction(input: z.infer<typeof walkIn>) {
  const parsed = walkIn.parse(input);
  const result = await runAction(() => issueWalkInTicket(parsed));

  revalidatePath(QUEUE_PATH);

  return result;
}

/** The pick-list behind the walk-in form. A read, exposed as an action so a client component can ask. */
export async function searchPetsAction(query: string): Promise<ActionResult<PetSummary[]>> {
  const text = z.string().trim().max(100).parse(query);

  if (text.length < 2) {
    return { ok: true, data: [] };
  }

  return runAction(() => searchPets(text));
}

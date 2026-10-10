'use server';

import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { askAssistant } from '../api/askAssistant';
import { draftOwnerMessage } from '../api/draftOwnerMessage';
import { draftSoap } from '../api/draftSoap';
import { summarizePet } from '../api/summarizePet';

/**
 * The staff AI assistant's calls (docs/contract-extensions.md § 34). Each one checks the shape,
 * makes one call and hands the draft back. None of them saves or sends anything: the person uses
 * the draft through the page's ordinary forms.
 */

const id = z.string().min(1).max(64);

/** A one-paragraph briefing on a patient, with citations. */
export async function summarizePetAction(petId: string) {
  return runAction(() => summarizePet(id.parse(petId)));
}

const soapInput = z.object({ consultationId: id, notes: z.string().trim().min(1).max(4000) });

/** The vet's short notes as a draft of the case's own fields. */
export async function draftSoapAction(input: z.infer<typeof soapInput>) {
  const parsed = soapInput.parse(input);
  return runAction(() => draftSoap(parsed.consultationId, parsed.notes));
}

/** Plain-language aftercare instructions from a case, to copy. */
export async function draftAftercareAction(consultationId: string) {
  return runAction(() => draftOwnerMessage({ kind: 'aftercare', consultation_id: id.parse(consultationId) }));
}

/** A reply to a customer's thread, to put in the reply box. */
export async function draftThreadReplyAction(threadId: string) {
  return runAction(() => draftOwnerMessage({ kind: 'thread_reply', thread_id: z.string().min(1).max(40).parse(threadId) }));
}

const conversation = z
  .array(z.object({ role: z.enum(['user', 'assistant']), text: z.string().trim().min(1).max(1000) }))
  .min(1)
  .max(20);

/** The next answer about the clinic's numbers. Only console paths become buttons. */
export async function askAction(messages: z.infer<typeof conversation>) {
  const parsed = conversation.parse(messages);
  const result = await runAction(() => askAssistant(parsed));

  if (!result.ok) return result;

  const actions = result.data.actions.filter((action) => action.href.startsWith('/') && !action.href.startsWith('//'));

  return { ok: true as const, data: { reply: result.data.reply, actions } };
}

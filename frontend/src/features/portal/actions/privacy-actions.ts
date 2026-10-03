'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { createDataRequest, saveConsents } from '../api/privacy';

const purpose = z.enum(['service_messages', 'marketing', 'medical_record_sharing', 'extended_data_processing']);

const consentsInput = z.object({ consents: z.partialRecord(purpose, z.boolean()) });

/** Saves the consent switches. Only the ones sent are recorded; the rest stay as they were. */
export async function saveConsentsAction(values: z.infer<typeof consentsInput>) {
  const { consents } = consentsInput.parse(values);
  const result = await runAction(() => saveConsents(consents));

  revalidatePath('/portal/account');
  return result.ok ? { ok: true as const } : result;
}

const requestInput = z.object({
  type: z.enum(['access', 'correction', 'erasure', 'portability']),
  detail: z.string().trim().max(2000).optional(),
});

/** Files a PDPA request with the clinic. Staff handle it; this only records it. */
export async function dataRequestAction(values: z.infer<typeof requestInput>) {
  const input = requestInput.parse(values);
  const result = await runAction(() => createDataRequest(input.type, input.detail || undefined));

  return result.ok ? { ok: true as const } : result;
}

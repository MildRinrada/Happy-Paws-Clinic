'use server';

import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { amendLabResult, voidLabResult } from '../api/lab-requests';
import { revalidateLab } from '../lib/revalidate-lab';

/**
 * Correcting one value (docs/contract-extensions.md § 52). Each sends the request's version as
 * `If-Match`, so a correction made on a page someone else has changed since is refused, not merged.
 */

const id = z.string().min(1).max(64);
const reason = z.string().trim().min(1).max(500);
const optionalText = (max: number) => z.string().trim().max(max).optional().transform((value) => value || undefined);
const optionalNumber = z.number().finite().optional();

const amend = z.object({
  labRequestId: id,
  resultId: id,
  version: z.number().int().nonnegative(),
  correction: z.object({
    reason,
    analyte: optionalText(100),
    value: optionalNumber,
    value_text: optionalText(500),
    unit: optionalText(30),
    reference_low: optionalNumber,
    reference_high: optionalNumber,
    reference_text: optionalText(200),
    abnormal: z.boolean().optional(),
  }),
});

/** Replaces one value with the corrected one. The value replaced stays on file. */
export async function amendLabResultAction(input: z.input<typeof amend>) {
  const parsed = amend.parse(input);
  const result = await runAction(() => amendLabResult(parsed.labRequestId, parsed.resultId, parsed.correction, parsed.version));

  revalidateLab();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const strike = z.object({ labRequestId: id, resultId: id, version: z.number().int().nonnegative(), reason });

/** Strikes one value out. It stays listed, crossed through, and counts towards nothing. */
export async function voidLabResultAction(input: z.input<typeof strike>) {
  const parsed = strike.parse(input);
  const result = await runAction(() => voidLabResult(parsed.labRequestId, parsed.resultId, parsed.reason, parsed.version));

  revalidateLab();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

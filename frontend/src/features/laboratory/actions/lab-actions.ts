'use server';

import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { createLabRequest, recordLabResults, reviewLabResults } from '../api/lab-requests';
import { revalidateLab } from '../lib/revalidate-lab';

/**
 * The lab's three everyday writes. What a request may hold and who may read it are the backend's
 * rules; these check the shape, make one call, and re-read the worklist and any open case page.
 */

const id = z.string().min(1).max(64);
const text = (max: number) => z.string().trim().max(max);
const optionalText = (max: number) => text(max).optional().transform((value) => value || undefined);

const order = z.object({
  pet_id: id,
  consultation_id: id.optional(),
  destination: z.enum(['in_house', 'external']),
  external_lab_name: optionalText(200),
  priority: z.enum(['routine', 'urgent']),
  clinical_notes: optionalText(2000),
  tests: z
    .array(z.object({ name: text(200).min(1), code: optionalText(50) }))
    .min(1)
    .max(30),
});

/** Orders the tests. An external lab must be named; the backend says so if it is not. */
export async function createLabRequestAction(input: z.input<typeof order>) {
  const parsed = order.parse(input);
  const result = await runAction(() =>
    createLabRequest({
      ...parsed,
      external_lab_name: parsed.destination === 'external' ? parsed.external_lab_name : undefined,
    }),
  );

  revalidateLab();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const optionalNumber = z.number().finite().optional();

const results = z.object({
  labRequestId: id,
  results: z
    .array(
      z.object({
        analyte: text(100).min(1),
        value: optionalNumber,
        value_text: optionalText(500),
        unit: optionalText(30),
        reference_low: optionalNumber,
        reference_high: optionalNumber,
        reference_text: optionalText(200),
        abnormal: z.boolean().optional(),
      }),
    )
    .min(1)
    .max(100),
});

/** Adds results. Each call adds to what is there; a value is corrected one at a time, with a reason. */
export async function recordLabResultsAction(input: z.input<typeof results>) {
  const parsed = results.parse(input);
  const result = await runAction(() => recordLabResults(parsed.labRequestId, parsed.results));

  revalidateLab();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const review = z.object({ labRequestId: id, note: optionalText(1000) });

/** Marks the results as read, with the vet's note (§ 53). Reading twice is fine; before results exist it is `409`. */
export async function reviewLabResultsAction(input: z.input<typeof review>) {
  const parsed = review.parse(input);
  const result = await runAction(() => reviewLabResults(parsed.labRequestId, parsed.note));

  revalidateLab();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

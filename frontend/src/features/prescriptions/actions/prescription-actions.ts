'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { dispensePrescription } from '../api/dispensePrescription';
import { prescriptionPaths } from '../lib/prescription-paths';

/**
 * The pharmacy counter's one write. Whether a prescription may be dispensed, and that it is
 * dispensed once, is the backend's rule; this checks the shape, makes the call and marks the page
 * stale. The label comes back in the result for the screen to print.
 */

const dispense = z.object({ prescriptionId: z.string().min(1).max(64) });

export async function dispensePrescriptionAction(input: z.infer<typeof dispense>) {
  const parsed = dispense.parse(input);
  const result = await runAction(() => dispensePrescription(parsed.prescriptionId));

  revalidatePath(prescriptionPaths.list);

  return result;
}

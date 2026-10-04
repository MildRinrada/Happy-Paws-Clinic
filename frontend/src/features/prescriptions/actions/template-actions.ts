'use server';

import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { listTreatmentTemplates } from '../api/listTreatmentTemplates';
import type { TreatmentTemplate } from '../types/treatment-template.types';

/**
 * The clinic's treatment templates, read on demand when the vet opens the picker on a case. A read
 * rather than a page load, so the case page does not pay for a list most visits never open.
 */
export async function listTreatmentTemplatesAction(): Promise<ActionResult<TreatmentTemplate[]>> {
  return runAction(() => listTreatmentTemplates());
}

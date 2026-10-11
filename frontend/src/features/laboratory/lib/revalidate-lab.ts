import { revalidatePath } from 'next/cache';
import { LAB_PATH } from './lab-filters';

/** After any lab write: the worklist, and any open case page, since a case shows its own requests. */
export function revalidateLab() {
  revalidatePath(LAB_PATH);
  revalidatePath('/consultations/[id]', 'page');
}

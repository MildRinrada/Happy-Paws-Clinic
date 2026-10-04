import type { PrescriptionItem } from '../types/prescription.types';

/**
 * What the counter still has to hand over goes first; within each half the backend's order (newest
 * first) is kept, since `Array.prototype.sort` is stable.
 */
export function undispensedFirst(items: PrescriptionItem[]): PrescriptionItem[] {
  return [...items].sort((a, b) => Number(Boolean(a.dispensed_at)) - Number(Boolean(b.dispensed_at)));
}

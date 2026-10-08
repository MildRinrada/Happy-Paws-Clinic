import type { InvoiceLineKind } from '../types/billing.types';

/** The Thai name of each line kind, as the bill and the line editor show it. */
export const lineKindLabel: Record<InvoiceLineKind, string> = {
  service: 'บริการ',
  treatment: 'หัตถการ',
  medicine: 'ยา',
  other: 'อื่น ๆ',
};

export const lineKinds: InvoiceLineKind[] = ['service', 'treatment', 'medicine', 'other'];

import { parseBahtToSatang } from '@/lib/format/money';
import type { InvoiceLineInput, InvoiceLineKind } from '../types/billing.types';

/** One line of a hand-raised bill, as typed. `key` keeps React's rows stable; it is never drawn. */
export interface InvoiceLineDraft {
  key: string;
  /** The catalogue entry it was filled from, or '' when typed by hand. */
  source: string;
  description: string;
  kind: InvoiceLineKind;
  quantity: string;
  unitPrice: string;
}

export const emptyLineDraft = (): InvoiceLineDraft => ({
  key: crypto.randomUUID(),
  source: '',
  description: '',
  kind: 'other',
  quantity: '1',
  unitPrice: '',
});

function parseQuantity(value: string): number | null {
  return /^\d+$/.test(value.trim()) && Number(value) > 0 ? Number(value) : null;
}

/** The line as the API wants it, or null while it is incomplete. */
export function toLineInput(line: InvoiceLineDraft): InvoiceLineInput | null {
  const quantity = parseQuantity(line.quantity);
  const unitPrice = parseBahtToSatang(line.unitPrice);
  const description = line.description.trim();

  if (!description || quantity === null || unitPrice === null) {
    return null;
  }

  return { description, quantity, unit_price_satang: unitPrice, kind: line.kind };
}

/**
 * What the lines typed so far add up to, in satang. A preview only: the bill's real totals are the
 * backend's, read back from the invoice once it exists. Incomplete lines count as nothing.
 */
export function draftSubtotal(lines: InvoiceLineDraft[]): number {
  return lines.reduce((sum, line) => {
    const input = toLineInput(line);
    return sum + (input ? input.quantity * input.unit_price_satang : 0);
  }, 0);
}

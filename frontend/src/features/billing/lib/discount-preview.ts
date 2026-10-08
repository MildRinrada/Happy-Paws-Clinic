import { parseBahtToSatang } from '@/lib/format/money';
import type { Invoice } from '../types/billing.types';

export type DiscountMode = 'amount' | 'percent';

export interface DiscountPreview {
  /** What would come off, in satang, or null while the input is not a number the API takes. */
  amountSatang: number | null;
  /** The percent as it would be sent, when the mode is percent. */
  percent?: number;
  /** Which local rule the figure breaks, if any. The backend checks the same rules again. */
  problem?: 'format' | 'tooLarge' | 'belowPaid';
  /** How much more may come off before the discounts pass the subtotal or the total drops below what was paid. */
  roomSatang: number;
}

/**
 * The discount a cashier is about to give, worked out the way the backend will: a percent is taken
 * off the subtotal (not the running total) and rounded down to the satang. Shown before saving so
 * the new total is not a surprise; the figure the bill keeps is the one the backend answers with.
 */
export function previewDiscount(invoice: Invoice, mode: DiscountMode, input: string): DiscountPreview {
  const room = Math.min(
    invoice.subtotal_satang - invoice.discount_satang,
    invoice.total_satang - invoice.paid_satang,
  );
  const roomSatang = Math.max(0, room);

  let amountSatang: number | null;
  let percent: number | undefined;

  if (mode === 'amount') {
    const parsed = parseBahtToSatang(input);
    amountSatang = parsed !== null && parsed > 0 ? parsed : null;
  } else {
    const cleaned = input.replace(/[\s%]/g, '');
    const value = /^\d+(\.\d{1,2})?$/.test(cleaned) ? Number(cleaned) : NaN;
    percent = value > 0 && value <= 100 ? value : undefined;
    amountSatang = percent === undefined ? null : Math.floor((invoice.subtotal_satang * percent) / 100);
  }

  if (amountSatang === null) {
    return { amountSatang, percent, problem: 'format', roomSatang };
  }

  if (invoice.discount_satang + amountSatang > invoice.subtotal_satang) {
    return { amountSatang, percent, problem: 'tooLarge', roomSatang };
  }

  if (invoice.total_satang - amountSatang < invoice.paid_satang) {
    return { amountSatang, percent, problem: 'belowPaid', roomSatang };
  }

  return { amountSatang, percent, roomSatang };
}

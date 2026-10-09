import { formatBaht } from '@/lib/format/money';

/** "฿100" for a round amount, the usual "฿99.50" otherwise. For rules lines, not for bills. */
export function formatWholeBaht(satang: number): string {
  return satang % 100 === 0 ? `฿${(satang / 100).toLocaleString('th-TH')}` : formatBaht(satang);
}

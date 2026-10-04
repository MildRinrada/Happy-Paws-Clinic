import { parseBahtToSatang } from '@/lib/format/money';

/** Satang as the plain number a price box starts with: 50000 is "500", 125050 is "1250.50". */
export function satangToInput(satang: number): string {
  return satang % 100 === 0 ? String(satang / 100) : (satang / 100).toFixed(2);
}

/** What was typed in a price box, in satang, or `null` when it is not a price. */
export function inputToSatang(input: string): number | null {
  return parseBahtToSatang(input);
}

/**
 * Money crosses the API as an integer number of satang (`146000` is ฿1,460.00) and stays one until
 * the moment it is drawn. Formatting is the only place it becomes a decimal.
 */

const baht = new Intl.NumberFormat('th-TH', {
  style: 'currency',
  currency: 'THB',
  minimumFractionDigits: 2,
});

export function formatBaht(satang: number): string {
  return baht.format(satang / 100);
}

/**
 * Parses what a cashier typed — "1,460", "1460.5" — into satang. `null` when it is not an amount.
 *
 * Rounded, not truncated: 14.6 × 100 is 1459.9999… in floating point, and truncating that would
 * take a satang off the customer's payment.
 */
export function parseBahtToSatang(input: string): number | null {
  const cleaned = input.replace(/[,\s฿]/g, '');

  if (!/^\d+(\.\d{1,2})?$/.test(cleaned)) {
    return null;
  }

  return Math.round(Number(cleaned) * 100);
}

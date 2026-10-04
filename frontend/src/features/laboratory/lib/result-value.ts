const NUMBER = /^-?\d+(\.\d+)?$/;

/** A typed number, or `undefined` for a blank or non-numeric entry. */
export function parseNumber(raw: string): number | undefined {
  const value = raw.trim();
  return NUMBER.test(value) ? Number(value) : undefined;
}

/**
 * Splits what was typed in the value box the way the API wants it: a number goes in `value`, so
 * the backend can flag it against the range, and anything else ("positive", "พบไข่พยาธิ") in
 * `value_text`.
 */
export function splitResultValue(raw: string): { value?: number; value_text?: string } {
  const value = raw.trim();
  if (!value) return {};

  const numeric = parseNumber(value);
  return numeric === undefined ? { value_text: value } : { value: numeric };
}

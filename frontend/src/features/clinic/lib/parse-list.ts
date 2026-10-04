/** "ห้อง 1, ห้อง 2,," as a list of the names typed, trimmed, blanks and repeats dropped. */
export function parseCommaList(input: string): string[] {
  return [...new Set(input.split(',').map((part) => part.trim()).filter(Boolean))];
}

/** One entry per line, the same way. */
export function parseLineList(input: string): string[] {
  return [...new Set(input.split('\n').map((part) => part.trim()).filter(Boolean))];
}

/** A number typed in a box, or `undefined` when the box is empty or not a number. */
export function optionalNumber(input: string): number | undefined {
  const trimmed = input.trim();
  if (trimmed === '') return undefined;

  const value = Number(trimmed);
  return Number.isFinite(value) ? value : undefined;
}

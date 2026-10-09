/** A whole number typed into a box, or null when it is empty or not one. */
export function parseCount(input: string): number | null {
  const cleaned = input.replace(/[,\s]/g, '');

  return /^\d+$/.test(cleaned) ? Number(cleaned) : null;
}

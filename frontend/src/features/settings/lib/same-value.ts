/**
 * True when two form values hold the same data. Form values here are plain objects, arrays,
 * strings, numbers and booleans built the same way each time, so their JSON is a fair comparison.
 */
export function sameValue(a: unknown, b: unknown): boolean {
  return a === b || JSON.stringify(a) === JSON.stringify(b);
}

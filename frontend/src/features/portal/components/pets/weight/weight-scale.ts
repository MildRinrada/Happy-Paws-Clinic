import type { WeightPoint } from '../../../lib/load-pet-weights';

/**
 * Each bar's height as a share of the tallest (0 to 1).
 *
 * A pet's weight moves by tenths of a kilo, so bars drawn from zero would all look the same. The
 * floor is set below the lightest weighing by one and a half times the spread, never below zero:
 * the lightest bar stands at about 60% of the heaviest, as the design draws 3.6 against 4.2, and a
 * puppy that tripled its weight still shows it. Every bar carries its value, in words for a screen
 * reader too, so the shortened scale cannot mislead.
 */
export function barShares(points: WeightPoint[]): number[] {
  const values = points.map((point) => point.kg);
  const top = Math.max(...values);
  const low = Math.min(...values);
  const floor = Math.max(0, low - (top - low) * 1.5);

  if (top <= floor) return values.map(() => 0.75);

  return values.map((kg) => (kg - floor) / (top - floor));
}

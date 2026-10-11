import { clinicDay } from '@/lib/format/datetime';
import type { MyPet } from '../types/portal.types';
import { loadPetRecords } from './load-pet-records';

/** One weighing: when (an ISO instant) and how heavy. */
export interface WeightPoint {
  date: string;
  kg: number;
}

/** How many weighings the chart draws, the newest ones. */
export const WEIGHT_POINTS = 6;

/**
 * A pet's recent weights, oldest first, at most `WEIGHT_POINTS`.
 *
 * For now they are pieced together from what the portal can already read: the weights in the
 * visit records (only when the clinic opens the `vitals` section and this owner may see history)
 * and the pet's latest weight. A portal weight history endpoint is proposed (ส่วนขยายข้อ 44, not
 * approved yet); when it exists, only this function changes. Never makes a point up: with fewer than
 * two, the page shows the latest weight without a chart.
 */
export async function loadPetWeights(pet: MyPet): Promise<{ points: WeightPoint[] }> {
  const records = await loadPetRecords(pet.id, pet.may_view_history);

  const fromRecords = (records?.visits ?? []).flatMap((visit) =>
    (visit.vitals ?? []).flatMap((vital) =>
      typeof vital.weight_kg === 'number' ? [{ date: vital.recorded_at, kg: vital.weight_kg }] : [],
    ),
  );
  const latest = pet.latest_weight ? [{ date: pet.latest_weight.recorded_at, kg: pet.latest_weight.weight_kg }] : [];

  // One point per clinic day, the last weighing of that day, so a weight recorded twice is drawn once.
  const byDay = new Map<string, WeightPoint>();
  for (const point of [...fromRecords, ...latest].sort((a, b) => Date.parse(a.date) - Date.parse(b.date))) {
    byDay.set(clinicDay(point.date), point);
  }

  return { points: [...byDay.values()].slice(-WEIGHT_POINTS) };
}

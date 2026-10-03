import type { Slot } from '../types/booking.types';

/** One start time, and every vet free at it. */
export interface SlotTime {
  startsAt: string;
  staffIds: string[];
}

/**
 * Folds the API's one-row-per-vet slots into one entry per time. With `vet` set, only that vet's
 * times are kept.
 */
export function slotTimes(slots: Slot[], vet?: string): SlotTime[] {
  const byTime = new Map<string, string[]>();

  for (const slot of slots) {
    if (vet && slot.staff_id !== vet) continue;
    byTime.set(slot.starts_at, [...(byTime.get(slot.starts_at) ?? []), slot.staff_id]);
  }

  return [...byTime].map(([startsAt, staffIds]) => ({ startsAt, staffIds }));
}

/** The vets who have at least one free time that day, in the order they first appear. */
export function vetsIn(slots: Slot[]): string[] {
  return [...new Set(slots.map((slot) => slot.staff_id))];
}

/**
 * `details.suggested` from a `slot_unavailable` refusal: "HH:mm" strings, or nothing. The backend
 * may list one time once per free vet, so repeats are dropped, and only the first `limit` are kept:
 * a choice of a few nearby times, not the rest of the day.
 */
export function suggestedTimes(details: Record<string, unknown> | undefined, limit = 6): string[] {
  const suggested = details?.suggested;
  const times = Array.isArray(suggested) ? suggested.filter((item): item is string => typeof item === 'string') : [];

  return [...new Set(times)].slice(0, limit);
}

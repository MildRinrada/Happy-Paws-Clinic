import type { DayStatus, WeekSlots } from '../types/booking.types';

/**
 * Whether a day has a free time for the vet chosen (any vet when `vet` is empty): what its chip
 * says. `full` covers every day with no free time, booked out or closed alike, since the API
 * answers both with an empty list.
 */
export function dayStatus(week: WeekSlots, day: string, vet: string): DayStatus {
  if (week.status !== 'ready') return 'unknown';

  const slots = week.days[day];
  if (!slots?.ok) return 'unknown';

  return slots.slots.some((slot) => !vet || slot.staff_id === vet) ? 'free' : 'full';
}

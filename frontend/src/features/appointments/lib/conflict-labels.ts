import { formatTime } from '@/lib/format/datetime';
import type { CalendarItem } from '../types/calendar.types';

/**
 * Each appointment on the day as the line a person finds it by on the grid: its time, the pet and
 * the vet. Names the session may not read are left out, so the time is always there.
 */
export function conflictLabels(items: CalendarItem[]): Record<string, string> {
  return Object.fromEntries(
    items.map((item) => {
      const parts = [formatTime(item.appointment.starts_at), item.petName, item.vetName].filter(Boolean);
      return [item.appointment.id, parts.join(' / ')];
    }),
  );
}

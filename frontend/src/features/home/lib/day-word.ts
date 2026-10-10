import { addDays, dayChipParts } from '@/features/booking/lib/booking-dates';
import { clinicDay } from '@/lib/format/datetime';
import { nextFreeTime } from '../content/next-free-time';

/** "วันนี้" or "พรุ่งนี้" for a time, in the clinic's calendar; the date itself for any other day. */
export function dayWord(startsAt: string, now: number): string {
  const day = clinicDay(startsAt);
  const today = clinicDay(new Date(now));

  if (day === today) return nextFreeTime.today;
  if (day === addDays(today, 1)) return nextFreeTime.tomorrow;

  const { weekday, date } = dayChipParts(day);
  return `${weekday} ${date}`;
}

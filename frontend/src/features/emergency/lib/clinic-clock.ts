import type { Weekday } from '@/features/clinic/types/clinic.types';
import { clinicDay } from '@/lib/format/datetime';

/** "HH:mm" on the clinic's wall clock. `h23` so midnight is 00:00, never 24:00. */
const wallClock = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'Asia/Bangkok',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});

/** `Date.getUTCDay()` order: Sunday is 0. */
const FROM_SUNDAY: Weekday[] = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];

/** The clinic's day (`YYYY-MM-DD`) and time (`HH:mm`) at an instant, whatever the server's own zone. */
export function clinicClock(now: Date): { day: string; time: string } {
  return { day: clinicDay(now), time: wallClock.format(now) };
}

/** The weekday of a clinic day written `YYYY-MM-DD`. */
export function weekdayOf(day: string): Weekday {
  return FROM_SUNDAY[new Date(`${day}T00:00:00Z`).getUTCDay()]!;
}

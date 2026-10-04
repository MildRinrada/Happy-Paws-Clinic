import { addDays } from '@/features/booking/lib/booking-dates';
import { clinicDay } from '@/lib/format/datetime';

const DAY = /^\d{4}-\d{2}-\d{2}$/;

/** The Monday of the week a clinic day falls in. Days are `YYYY-MM-DD`, read at UTC midnight. */
export function mondayOf(day: string): string {
  const weekday = new Date(`${day}T00:00:00Z`).getUTCDay();
  return addDays(day, -((weekday + 6) % 7));
}

/** The week in view: the one holding `?week=`, or this week when the parameter is missing or not a date. */
export function rosterWeek(param: string | undefined, now: Date = new Date()): string[] {
  const anchor = param && DAY.test(param) && !Number.isNaN(Date.parse(param)) ? param : clinicDay(now);
  const monday = mondayOf(anchor);

  return Array.from({ length: 7 }, (_, index) => addDays(monday, index));
}

/** True when a span of time touches a clinic day at all. */
export function touchesDay(startsAt: string, endsAt: string, day: string): boolean {
  return clinicDay(startsAt) <= day && clinicDay(new Date(Date.parse(endsAt) - 1)) >= day;
}

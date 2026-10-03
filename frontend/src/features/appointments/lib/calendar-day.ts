import { addDays } from '@/features/booking/lib/booking-dates';
import { clinicDay } from '@/lib/format/datetime';

const DAY = /^\d{4}-\d{2}-\d{2}$/;

/** The `?date=` the calendar was asked for, or today in clinic time when it is missing or malformed. */
export function calendarDay(param: string | undefined, now: Date = new Date()): string {
  return param && DAY.test(param) && !Number.isNaN(Date.parse(`${param}T00:00:00Z`)) ? param : clinicDay(now);
}

export const previousDay = (day: string) => addDays(day, -1);
export const nextDay = (day: string) => addDays(day, 1);

/** 0 for Sunday, as `Date.getUTCDay` counts, for a `YYYY-MM-DD` clinic day. */
export function weekdayIndex(day: string): number {
  return new Date(`${day}T00:00:00Z`).getUTCDay();
}

/** Minutes after midnight on the clinic's wall clock. Bangkok is UTC+7 all year. */
export function clinicMinutes(iso: string): number {
  const date = new Date(iso);
  return (date.getUTCHours() * 60 + date.getUTCMinutes() + 7 * 60) % (24 * 60);
}

/** "08:00" to 480. */
export function minutesOf(time: string): number {
  const [hours = 0, minutes = 0] = time.split(':').map(Number);
  return hours * 60 + minutes;
}

/** 480 to "08:00". */
export function timeOf(minutes: number): string {
  return `${String(Math.floor(minutes / 60)).padStart(2, '0')}:${String(minutes % 60).padStart(2, '0')}`;
}

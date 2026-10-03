import { clinicDay } from '@/lib/format/datetime';

/**
 * Day arithmetic on clinic days written as `YYYY-MM-DD`. Done on the string's UTC midnight so a
 * viewer's own time zone can never shift a day.
 */

const DAY_MS = 86_400_000;

function utcMidnight(day: string): Date {
  return new Date(`${day}T00:00:00Z`);
}

export function addDays(day: string, count: number): string {
  return new Date(utcMidnight(day).getTime() + count * DAY_MS).toISOString().slice(0, 10);
}

/** Today and the next `count - 1` days, in the clinic's calendar. */
export function nextClinicDays(count: number, from: Date = new Date()): string[] {
  const today = clinicDay(from);
  return Array.from({ length: count }, (_, index) => addDays(today, index));
}

const weekday = new Intl.DateTimeFormat('th-TH', { weekday: 'short', timeZone: 'UTC' });
const dayMonth = new Intl.DateTimeFormat('th-TH', { day: 'numeric', month: 'short', timeZone: 'UTC' });

/** "จ." and "6 ต.ค." for a day chip. */
export function dayChipParts(day: string): { weekday: string; date: string } {
  const date = utcMidnight(day);
  return { weekday: weekday.format(date), date: dayMonth.format(date) };
}

/**
 * A start time from a clinic day and an "HH:mm", as the instant the API expects. The clinic is in
 * Bangkok, which has no daylight saving, so the offset is fixed.
 */
export function clinicInstant(day: string, time: string): string {
  return `${day}T${time}:00+07:00`;
}

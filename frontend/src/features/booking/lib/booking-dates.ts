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

/** How many days the day chips show at once, and one read of free times covers. */
export const WEEK_DAYS = 7;

/** Whole days from `from` to `to`, both `YYYY-MM-DD`. Negative when `to` is earlier. */
export function daysBetween(from: string, to: string): number {
  return Math.round((utcMidnight(to).getTime() - utcMidnight(from).getTime()) / DAY_MS);
}

/**
 * The first day of the seven-day row that holds `day`, counting rows from `today`: today's row
 * starts today, the next one a week later. A day before today falls in today's row.
 */
export function weekStartFor(day: string, today: string): string {
  const offset = Math.max(0, daysBetween(today, day));
  return addDays(today, Math.floor(offset / WEEK_DAYS) * WEEK_DAYS);
}

/** Today and the next `count - 1` days, in the clinic's calendar. */
export function nextClinicDays(count: number, from: Date = new Date()): string[] {
  const today = clinicDay(from);
  return Array.from({ length: count }, (_, index) => addDays(today, index));
}

// Fixed tables, not Intl: the server's and the browser's ICU data disagree on Thai short names
// (Node gave "อาทิตย์" where browsers give "อา."), and a chip rendered on both must match.
const WEEKDAYS = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];
const MONTHS = ['ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.'];

/** "จ." and "6 ต.ค." for a day chip. */
export function dayChipParts(day: string): { weekday: string; date: string } {
  const date = utcMidnight(day);
  return {
    weekday: WEEKDAYS[date.getUTCDay()] ?? '',
    date: `${date.getUTCDate()} ${MONTHS[date.getUTCMonth()] ?? ''}`,
  };
}

/** The day of the month alone, for the big number on a day chip. */
export function dayOfMonth(day: string): number {
  return utcMidnight(day).getUTCDate();
}

/**
 * A start time from a clinic day and an "HH:mm", as the instant the API expects. The clinic is in
 * Bangkok, which has no daylight saving, so the offset is fixed.
 */
export function clinicInstant(day: string, time: string): string {
  return `${day}T${time}:00+07:00`;
}

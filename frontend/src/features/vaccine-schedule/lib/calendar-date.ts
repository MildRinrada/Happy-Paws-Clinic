/**
 * Arithmetic on calendar days written `YYYY-MM-DD`, with no time and no time zone.
 *
 * A birth date and a vaccination date are days, not instants, so they are computed as days: a
 * day plus 7 is the same day of the week, whatever the clock or the viewer's laptop says. "Today"
 * comes from `clinicDay()` (Asia/Bangkok). Two such strings compare correctly as plain strings.
 */

const DAY_MS = 86_400_000;

/** The year, month (1 to 12) and day of a `YYYY-MM-DD` string. */
function parts(day: string): [number, number, number] {
  const [year = NaN, month = NaN, date = NaN] = day.split('-').map(Number);
  return [year, month, date];
}

function toUtc(day: string): number {
  const [year, month, date] = parts(day);
  return Date.UTC(year, month - 1, date);
}

function fromUtc(ms: number): string {
  return new Date(ms).toISOString().slice(0, 10);
}

/** True for a real day written `YYYY-MM-DD`, as `<input type="date">` gives one. */
export function isCalendarDay(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}$/.test(value) && fromUtc(toUtc(value)) === value;
}

export function addDays(day: string, days: number): string {
  return fromUtc(toUtc(day) + days * DAY_MS);
}

/** Months later, on the same day of the month or the month's last day if it has fewer. */
export function addMonths(day: string, months: number): string {
  const [year, month, date] = parts(day);
  const firstOfTarget = new Date(Date.UTC(year, month - 1 + months, 1));
  const lastDay = new Date(Date.UTC(firstOfTarget.getUTCFullYear(), firstOfTarget.getUTCMonth() + 1, 0)).getUTCDate();
  firstOfTarget.setUTCDate(Math.min(date, lastDay));
  return fromUtc(firstOfTarget.getTime());
}

export function daysBetween(from: string, to: string): number {
  return Math.round((toUtc(to) - toUtc(from)) / DAY_MS);
}

/** Whole months from one day to a later one. */
export function monthsBetween(from: string, to: string): number {
  const [fy, fm, fd] = parts(from);
  const [ty, tm, td] = parts(to);
  return (ty - fy) * 12 + (tm - fm) - (td < fd ? 1 : 0);
}

export const laterOf = (a: string, b: string): string => (a > b ? a : b);

// Thai months and the Buddhist-era year, as the rest of the site shows dates. The day is read in
// UTC because it was built in UTC above; there is no instant here to shift into another zone.
const thaiDay = new Intl.DateTimeFormat('th-TH', { timeZone: 'UTC', day: 'numeric', month: 'long', year: 'numeric' });

/** "26 กุมภาพันธ์ 2569". */
export function formatThaiDay(day: string): string {
  return thaiDay.format(new Date(toUtc(day)));
}


import { dashboardPage } from '../content/dashboard-page';
import type { Granularity } from '../types/analytics.types';

/**
 * Number formatting for the reports. Money uses `formatBaht` from `lib/format/money`, like the
 * cash desk; these cover the rest.
 */

const integer = new Intl.NumberFormat('th-TH');

const shortDate = new Intl.DateTimeFormat('th-TH', { timeZone: 'UTC', day: 'numeric', month: 'short' });
const monthYear = new Intl.DateTimeFormat('th-TH', { timeZone: 'UTC', month: 'short', year: '2-digit' });

/** Shown where the API sent nothing to measure. */
export const NO_VALUE = '-';

export function formatCount(value: number): string {
  return integer.format(value);
}

/** A rate from the API (0 to 1) as a percentage to one place. */
export function formatPercent(rate: number | null | undefined): string {
  return rate == null ? NO_VALUE : `${(rate * 100).toLocaleString('th-TH', { maximumFractionDigits: 1 })}%`;
}

export function formatMinutes(minutes: number | null | undefined): string {
  return minutes == null
    ? NO_VALUE
    : `${minutes.toLocaleString('th-TH', { maximumFractionDigits: 1 })} ${dashboardPage.minutes}`;
}

/** A clinic day `YYYY-MM-DD` as "4 ต.ค.". The date is a calendar day, so it is drawn in UTC. */
export function formatDay(day: string): string {
  return shortDate.format(new Date(`${day}T00:00:00Z`));
}

/** The first day of a month as "ต.ค. 69". */
export function formatMonth(day: string): string {
  return monthYear.format(new Date(`${day}T00:00:00Z`));
}

/** A series period's first day, drawn as a day or as a month. A week is named by its Monday. */
export function formatPeriod(period: string, granularity: Granularity): string {
  return granularity === 'month' ? formatMonth(period) : formatDay(period);
}

/** An hour of the clinic day as "09:00". */
export function formatHour(hour: number): string {
  return `${String(hour).padStart(2, '0')}:00`;
}

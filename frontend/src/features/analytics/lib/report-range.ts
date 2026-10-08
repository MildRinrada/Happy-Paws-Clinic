import { clinicDay } from '@/lib/format/datetime';
import type { Granularity } from '../types/analytics.types';

/** The two report tabs under today's cards. */
export type ReportTab = 'operations' | 'business';

/** What the page reads from its address: `/dashboard?tab=&from=&to=&granularity=`. */
export interface ReportRange {
  tab: ReportTab;
  from: string;
  to: string;
  granularity: Granularity;
}

export interface ReportSearchParams {
  tab?: string;
  from?: string;
  to?: string;
  granularity?: string;
}

/** The API refuses a longer span; the page clamps instead of showing that refusal. */
const LONGEST_SPAN_DAYS = 366;
const DEFAULT_SPAN_DAYS = 30;

const isDay = (value: string | undefined): value is string =>
  !!value && /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(Date.parse(`${value}T00:00:00Z`));

/** Calendar arithmetic on `YYYY-MM-DD`, done in UTC so no local zone can shift the day. */
export function shiftDay(day: string, days: number): string {
  const date = new Date(`${day}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

function spanDays(from: string, to: string): number {
  return Math.round((Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / 86_400_000) + 1;
}

/**
 * Reads the range from the address, falling back to the last 30 days. Anything malformed falls
 * back too: a typo in a bookmarked link should still open the page.
 */
export function readReportRange(params: ReportSearchParams, today: string = clinicDay(new Date())): ReportRange {
  const tab: ReportTab = params.tab === 'business' ? 'business' : 'operations';
  const to = isDay(params.to) ? params.to : today;
  let from = isDay(params.from) ? params.from : shiftDay(to, 1 - DEFAULT_SPAN_DAYS);

  if (from > to) {
    from = to;
  }
  if (spanDays(from, to) > LONGEST_SPAN_DAYS) {
    from = shiftDay(to, 1 - LONGEST_SPAN_DAYS);
  }

  const granularity: Granularity =
    params.granularity === 'week' || params.granularity === 'month' ? params.granularity : 'day';

  return { tab, from, to, granularity };
}

/** The page's own address for a range, keeping everything not overridden. */
export function reportHref(range: ReportRange, change: Partial<ReportRange> = {}): string {
  const next = { ...range, ...change };
  const query = new URLSearchParams({ tab: next.tab, from: next.from, to: next.to, granularity: next.granularity });

  return `/dashboard?${query.toString()}`;
}

/** Quick spans that end today. */
export function presetRanges(today: string): { key: string; from: string; to: string }[] {
  return [
    { key: 'last7', from: shiftDay(today, -6), to: today },
    { key: 'last30', from: shiftDay(today, -29), to: today },
    { key: 'last90', from: shiftDay(today, -89), to: today },
    { key: 'thisMonth', from: `${today.slice(0, 8)}01`, to: today },
  ];
}

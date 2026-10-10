import { addDays } from '@/features/booking/lib/booking-dates';
import { FORECAST_SPANS } from '../content/forecast-page';

const DAY = /^\d{4}-\d{2}-\d{2}$/;

type SearchParams = Record<string, string | string[] | undefined>;

const first = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

/**
 * `?from=&days=` of the forecast page. A day before today, or one that is not a date, starts at
 * today: the forecast only looks forward. A span other than the offered ones is a week.
 */
export function readForecastRange(params: SearchParams, today: string): { from: string; to: string; days: number } {
  const asked = first(params.from);
  const from = asked && DAY.test(asked) && !Number.isNaN(Date.parse(asked)) && asked >= today ? asked : today;
  const span = Number(first(params.days));
  const days = FORECAST_SPANS.includes(span) ? span : FORECAST_SPANS[0]!;

  return { from, to: addDays(from, days - 1), days };
}

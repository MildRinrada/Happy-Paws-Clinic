import type { InventoryForecastItem } from '../types/forecast.types';

/** The cover periods offered, in days. The backend accepts 1 to 180. */
export const COVER_CHOICES = [14, 30, 60, 90];
export const DEFAULT_COVER = 30;

export type ForecastSort = 'days' | 'order' | 'name';
export const FORECAST_SORTS: ForecastSort[] = ['days', 'order', 'name'];

type SearchParams = Record<string, string | string[] | undefined>;

const first = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

/** `?cover_days=&sort=` of the forecast page, falling back to the defaults for anything else. */
export function readForecastQuery(params: SearchParams): { coverDays: number; sort: ForecastSort } {
  const cover = Number(first(params.cover_days));
  const sort = FORECAST_SORTS.find((value) => value === first(params.sort));

  return { coverDays: COVER_CHOICES.includes(cover) ? cover : DEFAULT_COVER, sort: sort ?? 'days' };
}

/**
 * The rows in the order asked for. The backend already sends soonest-out first, items nobody uses
 * last; the other two orders are made here.
 */
export function sortForecast(rows: InventoryForecastItem[], sort: ForecastSort): InventoryForecastItem[] {
  if (sort === 'order') {
    return [...rows].sort((a, b) => b.suggested_order_qty - a.suggested_order_qty || a.name.localeCompare(b.name, 'th'));
  }

  if (sort === 'name') {
    return [...rows].sort((a, b) => a.name.localeCompare(b.name, 'th'));
  }

  return rows;
}

/** Runs out before an order placed today would arrive. */
export const runsOutBeforeDelivery = (row: InventoryForecastItem, leadTimeDays: number) =>
  row.days_until_out !== undefined && row.days_until_out <= leadTimeDays;

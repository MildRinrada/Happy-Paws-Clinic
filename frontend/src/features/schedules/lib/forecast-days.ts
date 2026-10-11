import { addDays } from '@/features/booking/lib/booking-dates';
import type { DemandForecast } from '../types/forecast.types';

/** The demand forecast looks forward only, two weeks at most (docs/contract-extensions.md § 33). */
const FORECAST_DAYS = 14;

/** The part of the week the forecast can cover: from today on, or `null` when the week is past or too far ahead. */
export function forecastSpan(days: string[], today: string): { from: string; to: string } | null {
  const from = days[0]! > today ? days[0]! : today;
  const last = addDays(today, FORECAST_DAYS - 1);
  const to = days.at(-1)! < last ? days.at(-1)! : last;

  return from <= to ? { from, to } : null;
}

/** The days the forecast expects to be heavy: any hour with more patients per vet than usual, or none on. */
export function busyDays(forecast: DemandForecast | null): Set<string> {
  return new Set((forecast?.days ?? []).filter((day) => day.high_load_hours > 0).map((day) => day.date));
}

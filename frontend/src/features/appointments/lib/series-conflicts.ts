import type { SeriesConflict } from '../types/advanced-booking.types';

/**
 * `details.conflicts` of a refused course, keyed by each start's time in ms so a row can find its
 * own whatever offset the backend wrote the instant with. Anything not shaped like a conflict is
 * left out rather than guessed at.
 */
export function seriesConflicts(details: Record<string, unknown> | undefined): Map<number, SeriesConflict> {
  const found = new Map<number, SeriesConflict>();
  const list = details?.conflicts;
  if (!Array.isArray(list)) return found;

  for (const item of list) {
    if (!item || typeof item !== 'object') continue;
    const { starts_at, reason, suggested } = item as Record<string, unknown>;
    if (typeof starts_at !== 'string' || typeof reason !== 'string') continue;

    found.set(Date.parse(starts_at), {
      starts_at,
      reason,
      suggested: Array.isArray(suggested) ? suggested.filter((time): time is string => typeof time === 'string') : [],
    });
  }

  return found;
}

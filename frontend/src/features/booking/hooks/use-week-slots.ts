'use client';

import { useEffect, useState } from 'react';
import { loadWeekSlotsAction } from '../actions/booking-actions';
import { bookingPage } from '../content/booking-page';
import type { DaySlots, WeekSlots } from '../types/booking.types';

/** How long a week already read is reused, so stepping back and forth does not read it again. */
const FRESH_MS = 60_000;

// In the browser only: the effect below is the one place it is read or written.
const cache = new Map<string, { at: number; days: Record<string, DaySlots> }>();

/**
 * Drops every week read so far. Called when the backend says a time was taken after all, so the
 * times shown next are read again rather than reused.
 */
export function forgetWeekSlots(): void {
  cache.clear();
}

/**
 * The free times of the seven days from `from`, for one service at one branch, read in one call
 * (`loadWeekSlotsAction`). A week read in the last minute is reused. Starts as `loading` on the
 * server and in the browser alike, so the first render always matches.
 */
export function useWeekSlots(serviceId: string, branchId: string, from: string): WeekSlots {
  const key = `${serviceId}|${branchId}|${from}`;
  // Tagged with the week it answers, so a turn to another week reads as loading at once rather
  // than showing the last week's days for one render.
  const [state, setState] = useState<{ key: string; value: WeekSlots }>({ key, value: { status: 'loading' } });

  useEffect(() => {
    const kept = cache.get(key);

    if (kept && Date.now() - kept.at < FRESH_MS) {
      setState({ key, value: { status: 'ready', days: kept.days } });
      return;
    }

    let current = true;
    setState({ key, value: { status: 'loading' } });

    loadWeekSlotsAction({ serviceId, branchId, from })
      .then((rows) => {
        const days: Record<string, DaySlots> = Object.fromEntries(
          rows.map(({ date, result }) => [
            date,
            result.ok ? { ok: true, slots: result.data.slots } : { ok: false, message: result.error.message },
          ]),
        );

        // Only a week read in full is kept; a day that failed is asked again next time.
        if (rows.every(({ result }) => result.ok)) cache.set(key, { at: Date.now(), days });
        if (current) setState({ key, value: { status: 'ready', days } });
      })
      .catch(() => current && setState({ key, value: { status: 'error', message: bookingPage.unreachable } }));

    return () => {
      current = false;
    };
  }, [key, serviceId, branchId, from]);

  return state.key === key ? state.value : { status: 'loading' };
}

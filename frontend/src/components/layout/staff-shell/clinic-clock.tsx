'use client';

import { useEffect, useState } from 'react';
import { dayChipParts } from '@/features/booking/lib/booking-dates';
import type { OpeningHours } from '@/features/clinic/types/clinic.types';
import { clinicClock } from '@/features/emergency/lib/clinic-clock';
import { openState } from '@/features/emergency/lib/open-status';
import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';

interface ClinicClockProps {
  /** The instant the server drew the page, so the first render matches on both sides. */
  now: string;
  /** The clinic's hours, once read. Without them only the date and time are shown. */
  hours?: OpeningHours | null;
  branchId?: string;
}

/** Milliseconds to the start of the next minute, plus a little so the tick lands inside it. */
function untilNextMinute(at: Date): number {
  return 60_000 - (at.getTime() % 60_000) + 50;
}

/**
 * The clinic's date and time in the top bar, and whether the branch in view is open, ticking over
 * each minute. Always clinic time (Asia/Bangkok), whatever the computer's own zone.
 *
 * The first render uses the server's instant, and the names come from fixed tables rather than
 * Thai `Intl` short names, so the browser draws exactly what the server sent and hydration never
 * disagrees. The browser's own clock takes over straight after.
 */
export function ClinicClock({ now, hours, branchId }: ClinicClockProps) {
  const [instant, setInstant] = useState(() => new Date(now));
  const text = staffShellText.clock;

  useEffect(() => {
    let timer: number | undefined;

    const tick = () => {
      const current = new Date();
      setInstant(current);
      timer = window.setTimeout(tick, untilNextMinute(current));
    };

    tick();
    return () => window.clearTimeout(timer);
  }, []);

  const { day, time } = clinicClock(instant);
  const { weekday, date } = dayChipParts(day);
  const state = hours ? openState(hours, branchId, instant).state : null;

  const status =
    state?.kind === 'open'
      ? { label: text.open(state.closes), tone: 'bg-emerald-50 text-emerald-800 ring-emerald-200' }
      : state?.kind === 'opens-later' && state.offset === 0
        ? { label: text.opensLater(state.opens), tone: 'bg-amber-50 text-amber-900 ring-amber-200' }
        : state
          ? { label: text.closed, tone: 'bg-ink-100 text-ink-700 ring-ink-200' }
          : null;

  return (
    <div className="flex items-center gap-2 whitespace-nowrap text-sm text-ink-600">
      <time dateTime={instant.toISOString()} aria-label={`${text.label} ${weekday} ${date} ${time}`} className="tabular-nums">
        {weekday} {date} <span className="font-semibold text-ink-800">{time}</span>
      </time>
      {status && <span className={`rounded-full px-2 py-0.5 text-xs font-medium ring-1 ${status.tone}`}>{status.label}</span>}
    </div>
  );
}

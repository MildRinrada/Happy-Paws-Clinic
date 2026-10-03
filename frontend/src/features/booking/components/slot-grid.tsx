'use client';

import { formatTime } from '@/lib/format/datetime';
import type { SlotTime } from '../lib/slot-times';

interface SlotGridProps {
  times: SlotTime[];
  disabled: boolean;
  onPick: (time: SlotTime) => void;
}

/** The free times as buttons, several to a row. Picking one holds it straight away. */
export function SlotGrid({ times, disabled, onPick }: SlotGridProps) {
  return (
    <ul className="grid grid-cols-3 gap-2 sm:grid-cols-5">
      {times.map((time) => (
        <li key={time.startsAt}>
          <button
            type="button"
            onClick={() => onPick(time)}
            disabled={disabled}
            className="h-11 w-full rounded-xl border border-ink-200 bg-white text-sm font-semibold text-ink-900 transition hover:border-site-teal hover:text-site-teal disabled:cursor-not-allowed disabled:opacity-50"
          >
            {formatTime(time.startsAt)}
          </button>
        </li>
      ))}
    </ul>
  );
}

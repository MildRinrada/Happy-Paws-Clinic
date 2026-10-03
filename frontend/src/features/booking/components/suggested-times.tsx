'use client';

import { bookingPage } from '../content/booking-page';

interface SuggestedTimesProps {
  times: string[];
  disabled: boolean;
  onPick: (time: string) => void;
}

/** The nearby times the backend offered when the one picked was taken. Nothing when it offered none. */
export function SuggestedTimes({ times, disabled, onPick }: SuggestedTimesProps) {
  if (times.length === 0) return null;

  return (
    <div className="flex flex-wrap items-center gap-2 text-sm">
      <span className="text-site-muted">{bookingPage.suggested}</span>
      {times.map((time) => (
        <button
          key={time}
          type="button"
          onClick={() => onPick(time)}
          disabled={disabled}
          className="rounded-full border border-site-teal px-3 py-1 font-semibold text-site-teal hover:bg-brand-50 disabled:opacity-50"
        >
          {time}
        </button>
      ))}
    </div>
  );
}

'use client';

import { bookingPage } from '../content/booking-page';
import { dayChipParts, nextClinicDays } from '../lib/booking-dates';

/** How many day chips to offer before the free date field. */
const CHIP_DAYS = 14;

interface DayPickerProps {
  value: string;
  onChange: (day: string) => void;
}

/**
 * Two weeks of days as chips to tap, and a date field for anything further. How far ahead the
 * clinic books is the backend's rule; a day beyond it simply comes back with no times.
 */
export function DayPicker({ value, onChange }: DayPickerProps) {
  const days = nextClinicDays(CHIP_DAYS);

  return (
    <div className="space-y-3">
      <ul className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
        {days.map((day) => {
          const { weekday, date } = dayChipParts(day);
          const selected = day === value;

          return (
            <li key={day} className="shrink-0">
              <button
                type="button"
                onClick={() => onChange(day)}
                aria-pressed={selected}
                className={`flex w-16 flex-col items-center rounded-xl border py-2 text-xs transition ${
                  selected ? 'border-site-teal bg-site-teal text-white' : 'border-ink-200 bg-white text-ink-700 hover:border-ink-300'
                }`}
              >
                <span>{weekday}</span>
                <span className="text-sm font-semibold">{date}</span>
              </button>
            </li>
          );
        })}
      </ul>

      <label className="flex items-center gap-3 text-sm text-ink-700">
        {bookingPage.time.otherDay}
        <input
          type="date"
          value={value}
          min={days[0]}
          onChange={(event) => event.target.value && onChange(event.target.value)}
          className="h-10 rounded-lg border border-ink-200 bg-white px-3 text-sm text-ink-900"
        />
      </label>
    </div>
  );
}

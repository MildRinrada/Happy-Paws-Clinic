'use client';

import { dayChipParts, dayOfMonth } from '@/features/booking/lib/booking-dates';

interface DayChipsProps {
  days: string[];
  today: string;
  selected: string;
  onSelect: (day: string) => void;
}

/** The week's seven days as chips on a phone; one is picked at a time. Names come from fixed tables, not `Intl`. */
export function DayChips({ days, today, selected, onSelect }: DayChipsProps) {
  return (
    <div className="grid grid-cols-7 gap-1">
      {days.map((day) => {
        const picked = day === selected;

        return (
          <button
            key={day}
            type="button"
            aria-pressed={picked}
            aria-label={`${dayChipParts(day).weekday} ${dayChipParts(day).date}`}
            className={`flex flex-col items-center rounded-lg border py-1.5 text-xs ${
              picked
                ? 'border-brand-600 bg-brand-600 text-white'
                : day === today
                  ? 'border-brand-300 bg-brand-50 text-brand-800'
                  : 'border-ink-200 bg-white text-ink-700'
            }`}
            onClick={() => onSelect(day)}
          >
            <span>{dayChipParts(day).weekday}</span>
            <span className="text-base font-semibold tabular-nums">{dayOfMonth(day)}</span>
          </button>
        );
      })}
    </div>
  );
}

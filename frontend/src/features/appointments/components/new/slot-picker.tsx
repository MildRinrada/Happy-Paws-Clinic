'use client';

import { formatTime } from '@/lib/format/datetime';
import type { SlotTime } from '@/features/booking/lib/slot-times';
import { newAppointment } from '../../content/appointment-calendar';

interface SlotPickerProps {
  times: SlotTime[] | null;
  loading: boolean;
  chosen?: string;
  onChoose: (startsAt: string) => void;
}

/** The free start times that day, as buttons. Nothing chosen until one is pressed. */
export function SlotPicker({ times, loading, chosen, onChoose }: SlotPickerProps) {
  const text = newAppointment.slots;

  if (loading) return <p className="text-sm text-ink-500">{text.loading}</p>;
  if (!times) return null;
  if (times.length === 0) return <p className="text-sm text-ink-500">{text.none}</p>;

  return (
    <div role="radiogroup" aria-label={text.label} className="grid grid-cols-4 gap-2 sm:grid-cols-6">
      {times.map((slot) => {
        const selected = slot.startsAt === chosen;

        return (
          <button
            key={slot.startsAt}
            type="button"
            role="radio"
            aria-checked={selected}
            onClick={() => onChoose(slot.startsAt)}
            className={`h-9 rounded-lg border text-sm tabular-nums transition ${
              selected ? 'border-brand-600 bg-brand-600 text-white' : 'border-ink-200 bg-white text-ink-800 hover:border-brand-400'
            }`}
          >
            {formatTime(slot.startsAt)}
          </button>
        );
      })}
    </div>
  );
}

'use client';

import { useId } from 'react';
import { bookingFrame } from '../../content/booking-frame';
import { bookingPage } from '../../content/booking-page';

interface VetSelectProps {
  vetIds: string[];
  names: Record<string, string>;
  value: string;
  onChange: (vetId: string) => void;
}

/** "สัตวแพทย์": whoever is free that day (the default), or one of the vets free that day. */
export function VetSelect({ vetIds, names, value, onChange }: VetSelectProps) {
  const id = useId();

  return (
    <div className="space-y-2.5">
      <label htmlFor={id} className="block text-[15px] font-semibold text-site-forest">
        {bookingFrame.time.vet}
      </label>
      <select
        id={id}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="h-[45px] w-full rounded-[8px] border border-site-border bg-white px-4 text-[15px] text-ink-900 sm:w-[343px]"
      >
        <option value="">{bookingFrame.time.anyVet}</option>
        {vetIds.map((vetId) => (
          <option key={vetId} value={vetId}>
            {names[vetId] ?? bookingPage.vetFallback}
          </option>
        ))}
      </select>
    </div>
  );
}

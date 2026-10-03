'use client';

import { bookingPage } from '../content/booking-page';

interface VetFilterProps {
  vetIds: string[];
  names: Record<string, string>;
  value: string;
  onChange: (vetId: string) => void;
}

/** "คนไหนก็ได้", or one of the vets who has a free time that day. */
export function VetFilter({ vetIds, names, value, onChange }: VetFilterProps) {
  return (
    <label className="flex flex-wrap items-center gap-3 text-sm text-ink-700">
      {bookingPage.time.vet}
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="h-10 min-w-[200px] rounded-lg border border-ink-200 bg-white px-3 text-sm text-ink-900"
      >
        <option value="">{bookingPage.time.anyVet}</option>
        {vetIds.map((vetId) => (
          <option key={vetId} value={vetId}>
            {names[vetId] ?? bookingPage.vetFallback}
          </option>
        ))}
      </select>
    </label>
  );
}

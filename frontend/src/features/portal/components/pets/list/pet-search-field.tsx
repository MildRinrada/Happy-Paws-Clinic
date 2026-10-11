'use client';

import { useId } from 'react';
import { portalPets } from '../../../content/portal-pets';

interface PetSearchFieldProps {
  value: string;
  onChange: (value: string) => void;
}

/** The search box over a long list of pets, with a magnifier inside it. */
export function PetSearchField({ value, onChange }: PetSearchFieldProps) {
  const id = useId();
  const text = portalPets.list;

  return (
    <div className="relative">
      <label htmlFor={id} className="sr-only">
        {text.searchLabel}
      </label>
      <svg
        viewBox="0 0 20 20"
        aria-hidden="true"
        className="pointer-events-none absolute left-3.5 top-1/2 size-[18px] -translate-y-1/2 text-ink-500"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
      >
        <circle cx="9" cy="9" r="5.5" />
        <path d="m13.2 13.2 3.8 3.8" />
      </svg>
      <input
        id={id}
        type="search"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={text.searchPlaceholder}
        className="h-11 w-full rounded-xl border border-ink-200 bg-white pl-10 pr-3.5 text-[15px] text-ink-900 placeholder:text-ink-400 focus:border-brand-500"
      />
    </div>
  );
}

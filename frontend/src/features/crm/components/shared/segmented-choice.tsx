'use client';

import { useId } from 'react';

interface Props<T extends string> {
  legend: string;
  /** Value to label, in the order to show them. */
  options: Record<T, string>;
  value: T;
  onChange: (value: T) => void;
}

/** One of a few choices, drawn as a row of buttons. Radio buttons underneath, so arrows and Tab work. */
export function SegmentedChoice<T extends string>({ legend, options, value, onChange }: Props<T>) {
  const name = useId();

  return (
    <fieldset className="space-y-1.5">
      <legend className="text-sm font-medium text-ink-700">{legend}</legend>
      <div className="inline-flex flex-wrap rounded-lg border border-ink-200 bg-white p-0.5">
        {(Object.keys(options) as T[]).map((option) => (
          <label
            key={option}
            className={`cursor-pointer rounded-md px-3 py-1.5 text-sm font-medium has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-brand-300 ${
              option === value ? 'bg-brand-50 text-brand-800 ring-1 ring-brand-200' : 'text-ink-600 hover:bg-ink-50'
            }`}
          >
            <input
              type="radio"
              name={name}
              value={option}
              checked={option === value}
              className="sr-only"
              onChange={() => onChange(option)}
            />
            {options[option]}
          </label>
        ))}
      </div>
    </fieldset>
  );
}

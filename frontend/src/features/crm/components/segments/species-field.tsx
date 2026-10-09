'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { segmentPage } from '../../content/segment-page';
import { MAX_SPECIES } from '../../lib/segment-draft';

interface Props {
  value: string[];
  onChange: (species: string[]) => void;
}

/** Which kinds of animal: the common ones as chips to tick, anything else typed in. */
export function SpeciesField({ value, onChange }: Props) {
  const text = segmentPage;
  const [other, setOther] = useState('');
  const full = value.length >= MAX_SPECIES;
  const extras = value.filter((name) => !text.speciesQuick.includes(name));

  function toggle(name: string) {
    if (value.includes(name)) onChange(value.filter((item) => item !== name));
    else if (!full) onChange([...value, name]);
  }

  function add() {
    const name = other.trim();
    if (name && !full && !value.some((item) => item.toLowerCase() === name.toLowerCase())) onChange([...value, name]);
    setOther('');
  }

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium text-ink-700">{text.species}</legend>
      <p className="text-sm text-ink-500">{text.speciesHint}</p>

      <div className="flex flex-wrap gap-2">
        {text.speciesQuick.map((name) => (
          <button
            key={name}
            type="button"
            aria-pressed={value.includes(name)}
            className={`rounded-full px-3 py-1 text-sm ring-1 ${
              value.includes(name) ? 'bg-brand-50 text-brand-800 ring-brand-200' : 'bg-white text-ink-700 ring-ink-200 hover:bg-ink-50'
            }`}
            onClick={() => toggle(name)}
          >
            {name}
          </button>
        ))}
        {extras.map((name) => (
          <span key={name} className="inline-flex items-center gap-1 rounded-full bg-brand-50 py-1 pl-3 pr-1 text-sm text-brand-800 ring-1 ring-brand-200">
            {name}
            <button
              type="button"
              aria-label={text.speciesRemove(name)}
              className="rounded-full px-1.5 text-brand-700 hover:bg-brand-100"
              onClick={() => toggle(name)}
            >
              ×
            </button>
          </span>
        ))}
      </div>

      <div className="flex max-w-sm gap-2">
        <label htmlFor="segment-species-other" className="sr-only">
          {text.speciesOther}
        </label>
        <Input
          id="segment-species-other"
          value={other}
          maxLength={60}
          placeholder={text.speciesOtherPlaceholder}
          disabled={full}
          className="min-w-0 flex-1"
          onChange={(event) => setOther(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              event.preventDefault();
              add();
            }
          }}
        />
        <Button variant="secondary" disabled={full || !other.trim()} onClick={add}>
          {text.speciesAdd}
        </Button>
      </div>
    </fieldset>
  );
}

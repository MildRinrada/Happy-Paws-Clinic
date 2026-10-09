'use client';

import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { segmentPage } from '../../content/segment-page';

interface Props {
  id: string;
  label: string;
  hint?: string;
  error?: string;
  value: string;
  /** Buttons that fill the box in one click. */
  quickDays?: number[];
  onChange: (value: string) => void;
}

/** A number of days, typed or picked from a few common ones. */
export function DayCountField({ id, label, hint, error, value, quickDays = [], onChange }: Props) {
  return (
    <Field label={label} htmlFor={id} hint={hint} error={error}>
      <div className="flex flex-wrap items-center gap-2">
        <Input
          id={id}
          type="number"
          min={1}
          max={3650}
          step={1}
          value={value}
          className="w-28"
          aria-invalid={error ? true : undefined}
          onChange={(event) => onChange(event.target.value)}
        />
        {quickDays.map((days) => (
          <Button key={days} size="sm" variant={value === String(days) ? 'primary' : 'secondary'} onClick={() => onChange(String(days))}>
            {segmentPage.quickDay(days)}
          </Button>
        ))}
      </div>
    </Field>
  );
}

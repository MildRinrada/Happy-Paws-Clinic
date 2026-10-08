'use client';

import { useId } from 'react';
import { Field, Input, Select } from '@/components/ui/field';
import { repeatText } from '../../content/advanced-booking';
import { MAX_VISITS, MIN_VISITS, type RepeatEnd, type RepeatFrequency, type RepeatRule } from '../../lib/recurrence';

interface RepeatFieldsProps {
  rule: RepeatRule;
  /** The first visit's day, the earliest the last day can be. */
  firstDay: string;
  onChange: (rule: RepeatRule) => void;
}

/** How often the course repeats and when it ends. The first visit is the date and time above. */
export function RepeatFields({ rule, firstDay, onChange }: RepeatFieldsProps) {
  const text = repeatText;
  const id = useId();
  const set = <K extends keyof RepeatRule>(field: K, value: RepeatRule[K]) => onChange({ ...rule, [field]: value });

  return (
    <div className="space-y-1.5">
      <div className="grid gap-3 sm:grid-cols-3">
        <Field label={text.frequency} htmlFor={`${id}-interval`}>
          <div className="flex gap-2">
            <Input
              id={`${id}-interval`}
              type="number"
              inputMode="numeric"
              min={1}
              max={52}
              value={rule.interval}
              className="h-9 w-20"
              onChange={(event) => set('interval', event.target.value)}
            />
            <Select
              aria-label={text.frequency}
              value={rule.frequency}
              className="h-9"
              onChange={(event) => set('frequency', event.target.value as RepeatFrequency)}
            >
              <option value="weekly">{text.weekly}</option>
              <option value="daily">{text.daily}</option>
            </Select>
          </div>
        </Field>

        <Field label={text.end} htmlFor={`${id}-end`}>
          <Select id={`${id}-end`} value={rule.end} className="h-9" onChange={(event) => set('end', event.target.value as RepeatEnd)}>
            <option value="count">{text.byCount}</option>
            <option value="until">{text.byUntil}</option>
          </Select>
        </Field>

        {rule.end === 'count' ? (
          <Field label={text.count} htmlFor={`${id}-count`}>
            <Input
              id={`${id}-count`}
              type="number"
              inputMode="numeric"
              min={MIN_VISITS}
              max={MAX_VISITS}
              value={rule.count}
              className="h-9 w-24"
              onChange={(event) => set('count', event.target.value)}
            />
          </Field>
        ) : (
          <Field label={text.until} htmlFor={`${id}-until`}>
            <Input
              id={`${id}-until`}
              type="date"
              min={firstDay || undefined}
              value={rule.until}
              className="h-9"
              onChange={(event) => set('until', event.target.value)}
            />
          </Field>
        )}
      </div>
      {rule.frequency === 'weekly' && <p className="text-xs text-ink-500">{text.weeklyNote}</p>}
    </div>
  );
}

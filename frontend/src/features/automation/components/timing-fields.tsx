'use client';

import { Field, Input, Select } from '@/components/ui/field';
import { automationsPage } from '../content/automations-page';
import { countsInDaysOnly } from '../lib/automation-form';
import type { AutomationTriggerType } from '../types/automation.types';

export interface TimingValue {
  mode: 'days' | 'hours';
  days: string;
  hours: string;
  time: string;
}

interface TimingFieldsProps {
  trigger: AutomationTriggerType;
  value: TimingValue;
  errors: Record<string, string>;
  onChange: (value: TimingValue) => void;
  /** Keeps the field ids apart when the create form and an edit form are on one page. */
  idPrefix?: string;
}

/**
 * When the rule sends: so many days before at a time of day, or so many hours before the start.
 * A vaccine's due date and a lapsed customer's due-back day have no time of day, so they only count
 * in days; the lapsed one only after the day (a negative number).
 */
export function TimingFields({ trigger, value, errors, onChange, idPrefix = 'automation' }: TimingFieldsProps) {
  const text = automationsPage.create;
  const daysOnly = countsInDaysOnly(trigger);
  const lapsed = trigger === 'customer_lapsed';
  const mode = daysOnly ? 'days' : value.mode;
  const set = (change: Partial<TimingValue>) => onChange({ ...value, ...change });

  return (
    <div className="grid gap-4 sm:grid-cols-3">
      {!daysOnly && (
        <Field label={text.timingMode} htmlFor={`${idPrefix}-mode`}>
          <Select id={`${idPrefix}-mode`} value={mode} onChange={(event) => set({ mode: event.target.value as TimingValue['mode'] })}>
            <option value="days">{text.modeDays}</option>
            <option value="hours">{text.modeHours}</option>
          </Select>
        </Field>
      )}

      {mode === 'days' ? (
        <>
          <Field
            label={lapsed ? text.offsetDaysLapsed : text.offsetDays}
            htmlFor={`${idPrefix}-days`}
            hint={lapsed ? text.offsetDaysLapsedHint : text.offsetDaysHint}
            error={errors['trigger.offset_days']}
          >
            <Input
              id={`${idPrefix}-days`}
              type="number"
              inputMode="numeric"
              min={lapsed ? -90 : undefined}
              max={lapsed ? -1 : undefined}
              value={value.days}
              onChange={(event) => set({ days: event.target.value })}
            />
          </Field>
          <Field label={text.sendTime} htmlFor={`${idPrefix}-time`} error={errors['trigger.send_time']}>
            <Input id={`${idPrefix}-time`} type="time" value={value.time} onChange={(event) => set({ time: event.target.value })} />
          </Field>
        </>
      ) : (
        <Field label={text.offsetHours} htmlFor={`${idPrefix}-hours`} error={errors['trigger.offset_hours']}>
          <Input id={`${idPrefix}-hours`} type="number" inputMode="numeric" min={1} max={168} value={value.hours} onChange={(event) => set({ hours: event.target.value })} />
        </Field>
      )}
    </div>
  );
}

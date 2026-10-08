'use client';

import { Field, Input, Select } from '@/components/ui/field';
import { automationsPage } from '../content/automations-page';
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
}

/**
 * When the rule sends: so many days before at a time of day, or so many hours before the start.
 * A vaccine's due date has no time of day, so it only counts in days.
 */
export function TimingFields({ trigger, value, errors, onChange }: TimingFieldsProps) {
  const text = automationsPage.create;
  const mode = trigger === 'vaccine_due' ? 'days' : value.mode;
  const set = (change: Partial<TimingValue>) => onChange({ ...value, ...change });

  return (
    <div className="grid gap-4 sm:grid-cols-3">
      {trigger !== 'vaccine_due' && (
        <Field label={text.timingMode} htmlFor="automation-mode">
          <Select id="automation-mode" value={mode} onChange={(event) => set({ mode: event.target.value as TimingValue['mode'] })}>
            <option value="days">{text.modeDays}</option>
            <option value="hours">{text.modeHours}</option>
          </Select>
        </Field>
      )}

      {mode === 'days' ? (
        <>
          <Field label={text.offsetDays} htmlFor="automation-days" hint={text.offsetDaysHint} error={errors['trigger.offset_days']}>
            <Input id="automation-days" type="number" inputMode="numeric" value={value.days} onChange={(event) => set({ days: event.target.value })} />
          </Field>
          <Field label={text.sendTime} htmlFor="automation-time" error={errors['trigger.send_time']}>
            <Input id="automation-time" type="time" value={value.time} onChange={(event) => set({ time: event.target.value })} />
          </Field>
        </>
      ) : (
        <Field label={text.offsetHours} htmlFor="automation-hours" error={errors['trigger.offset_hours']}>
          <Input id="automation-hours" type="number" inputMode="numeric" min={1} max={168} value={value.hours} onChange={(event) => set({ hours: event.target.value })} />
        </Field>
      )}
    </div>
  );
}

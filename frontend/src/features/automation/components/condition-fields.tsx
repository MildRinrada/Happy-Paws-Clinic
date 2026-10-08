'use client';

import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input } from '@/components/ui/field';
import { automationsPage } from '../content/automations-page';
import type { AutomationTriggerType } from '../types/automation.types';

export interface ConditionValue {
  species: string;
  vaccineName: string;
  unconfirmedOnly: boolean;
}

interface ConditionFieldsProps {
  trigger: AutomationTriggerType;
  value: ConditionValue;
  errors: Record<string, string>;
  onChange: (value: ConditionValue) => void;
}

/** The optional narrowing: a species, a vaccine (vaccine rules), unconfirmed visits only (visit rules). */
export function ConditionFields({ trigger, value, errors, onChange }: ConditionFieldsProps) {
  const text = automationsPage.create;
  const set = (change: Partial<ConditionValue>) => onChange({ ...value, ...change });

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <Field label={text.species} htmlFor="automation-species" hint={text.speciesHint} error={errors['condition.species']}>
        <Input id="automation-species" value={value.species} maxLength={200} onChange={(event) => set({ species: event.target.value })} />
      </Field>

      {trigger === 'vaccine_due' && (
        <Field label={text.vaccineName} htmlFor="automation-vaccine" hint={text.vaccineHint} error={errors['condition.vaccine_name']}>
          <Input id="automation-vaccine" value={value.vaccineName} maxLength={200} onChange={(event) => set({ vaccineName: event.target.value })} />
        </Field>
      )}

      {trigger === 'appointment_upcoming' && (
        <Checkbox
          className="self-end"
          label={text.unconfirmedOnly}
          checked={value.unconfirmedOnly}
          onChange={(event) => set({ unconfirmedOnly: event.target.checked })}
        />
      )}
    </div>
  );
}

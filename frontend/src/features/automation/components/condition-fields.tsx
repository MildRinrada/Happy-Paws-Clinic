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
  /** Keeps the field ids apart when the create form and an edit form are on one page. */
  idPrefix?: string;
}

/** The optional narrowing: a species, a vaccine (vaccine rules), unconfirmed visits only (visit rules). */
export function ConditionFields({ trigger, value, errors, onChange, idPrefix = 'automation' }: ConditionFieldsProps) {
  const text = automationsPage.create;
  const set = (change: Partial<ConditionValue>) => onChange({ ...value, ...change });

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <Field label={text.species} htmlFor={`${idPrefix}-species`} hint={text.speciesHint} error={errors['condition.species']}>
        <Input id={`${idPrefix}-species`} value={value.species} maxLength={200} onChange={(event) => set({ species: event.target.value })} />
      </Field>

      {trigger === 'vaccine_due' && (
        <Field label={text.vaccineName} htmlFor={`${idPrefix}-vaccine`} hint={text.vaccineHint} error={errors['condition.vaccine_name']}>
          <Input id={`${idPrefix}-vaccine`} value={value.vaccineName} maxLength={200} onChange={(event) => set({ vaccineName: event.target.value })} />
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

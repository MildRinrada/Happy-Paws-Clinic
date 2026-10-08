'use client';

import { Field, Input } from '@/components/ui/field';
import { automationsPage } from '../content/automations-page';

interface RuleNameFieldProps {
  id: string;
  value: string;
  error?: string;
  onChange: (value: string) => void;
}

/** The rule's name, as staff see it in the list. */
export function RuleNameField({ id, value, error, onChange }: RuleNameFieldProps) {
  const text = automationsPage.create;

  return (
    <Field label={text.name} htmlFor={id} error={error}>
      <Input id={id} value={value} maxLength={120} placeholder={text.namePlaceholder} onChange={(event) => onChange(event.target.value)} />
    </Field>
  );
}

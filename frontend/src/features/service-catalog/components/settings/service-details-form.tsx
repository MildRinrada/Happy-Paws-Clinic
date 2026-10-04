'use client';

import { useState, type FormEvent } from 'react';
import { Checkbox } from '@/components/ui/checkbox';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Input, Select } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateServiceAction } from '../../actions/service-actions';
import { serviceSettings } from '../../content/service-settings';
import type { ManagedService, ServiceCategory, ServiceDetailsInput } from '../../types/managed-service.types';

interface ServiceDetailsFormProps {
  service: ManagedService;
  categories: ServiceCategory[];
  /** Staff id to name, or `null` when this session cannot read the staff list. */
  staff: Record<string, string> | null;
  onDone: () => void;
}

/** Name, heading, length, who performs it, and whether it is on sale. Price has its own form. */
export function ServiceDetailsForm({ service, categories, staff, onDone }: ServiceDetailsFormProps) {
  const text = serviceSettings.details;
  const [values, setValues] = useState<ServiceDetailsInput>({
    name: service.name,
    category_code: service.category_code,
    duration_minutes: service.duration_minutes,
    // Someone who has since left is not on the active list and would be refused; they drop off here.
    eligible_staff_ids: staff ? service.eligible_staff_ids.filter((staffId) => staffId in staff) : service.eligible_staff_ids,
    is_active: service.is_active,
  });
  const { errors, message, pending, submit } = useFormSubmit(formMessages);
  const id = (field: string) => `service-${service.id}-${field}`;

  const set = <K extends keyof ServiceDetailsInput>(field: K, value: ServiceDetailsInput[K]) =>
    setValues((current) => ({ ...current, [field]: value }));

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => updateServiceAction({ id: service.id, version: service.version, values }), onDone);
  }

  return (
    <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
      <div className="grid gap-4 sm:grid-cols-3">
        <div className="sm:col-span-2">
          <Field label={text.name} htmlFor={id('name')} error={errors.name}>
            <Input id={id('name')} value={values.name} maxLength={200} required onChange={(event) => set('name', event.target.value)} />
          </Field>
        </div>

        <Field label={text.category} htmlFor={id('category')} error={errors.category_code}>
          <Select id={id('category')} value={values.category_code} onChange={(event) => set('category_code', event.target.value)}>
            {categories.map((category) => (
              <option key={category.code} value={category.code}>
                {category.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field label={text.duration} htmlFor={id('duration')} hint={text.durationHint} error={errors.duration_minutes}>
          <Input
            id={id('duration')}
            type="number"
            min={5}
            max={480}
            step={5}
            value={values.duration_minutes}
            required
            className="max-w-32"
            onChange={(event) => set('duration_minutes', Math.trunc(Number(event.target.value) || 0))}
          />
        </Field>
      </div>

      {staff ? (
        <CheckboxGroup
          legend={text.staff}
          hint={text.staffHint}
          error={errors.eligible_staff_ids}
          options={staff}
          selected={values.eligible_staff_ids}
          onChange={(selected) => set('eligible_staff_ids', selected)}
        />
      ) : (
        <p className="text-sm text-ink-500">{text.staffUnavailable}</p>
      )}

      <Checkbox label={text.active} hint={text.activeHint} checked={values.is_active} onChange={(event) => set('is_active', event.target.checked)} />

      <FormFooter pending={pending} message={message} onCancel={onDone} />
    </form>
  );
}

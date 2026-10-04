'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { addHolidayAction } from '../../actions/clinic-setup-actions';
import { hoursSettings } from '../../content/hours-settings';

interface AddHolidayFormProps {
  branches: { id: string; name: string }[];
  /** Today in clinic time: the earliest day a closure can be declared for. */
  today: string;
}

const blank = { date: '', name: '', branch_id: '' };

/** Declares a day closed. Booking for it stops at once; appointments already on it stay. */
export function AddHolidayForm({ branches, today }: AddHolidayFormProps) {
  const text = hoursSettings.holidays;
  const [open, setOpen] = useState(false);
  const [values, setValues] = useState(blank);
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  const set = (field: keyof typeof blank, value: string) => setValues((current) => ({ ...current, [field]: value }));

  if (!open) {
    return (
      <Button variant="secondary" onClick={() => setOpen(true)}>
        {text.add}
      </Button>
    );
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () => addHolidayAction({ date: values.date, name: values.name, branch_id: values.branch_id || undefined }),
      () => {
        setValues(blank);
        setOpen(false);
      },
    );
  }

  return (
    <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
      <div className="grid gap-4 sm:grid-cols-3">
        <Field label={text.date} htmlFor="holiday-date" error={errors.date}>
          <Input id="holiday-date" type="date" min={today} value={values.date} required onChange={(event) => set('date', event.target.value)} />
        </Field>

        <Field label={text.name} htmlFor="holiday-name" error={errors.name}>
          <Input
            id="holiday-name"
            value={values.name}
            maxLength={200}
            required
            placeholder={text.namePlaceholder}
            onChange={(event) => set('name', event.target.value)}
          />
        </Field>

        <Field label={text.branch} htmlFor="holiday-branch" error={errors.branch_id}>
          <Select id="holiday-branch" value={values.branch_id} onChange={(event) => set('branch_id', event.target.value)}>
            <option value="">{text.wholeClinic}</option>
            {branches.map((branch) => (
              <option key={branch.id} value={branch.id}>
                {branch.name}
              </option>
            ))}
          </Select>
        </Field>
      </div>

      <div className="flex flex-wrap items-center justify-end gap-3">
        <FormStatus message={message} savedText={formMessages.saved} />
        <Button variant="ghost" disabled={pending} onClick={() => setOpen(false)}>
          {formMessages.cancel}
        </Button>
        <Button type="submit" disabled={pending}>
          {pending ? formMessages.adding : formMessages.add}
        </Button>
      </div>
    </form>
  );
}

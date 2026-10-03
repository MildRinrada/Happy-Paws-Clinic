'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import type { ActionResult } from '@/lib/actions/action-result';
import { fieldErrors } from '@/lib/actions/field-errors';
import { contactSettings } from '../../content/contact-settings';
import type { ContactInput, ContactKind } from '../../types/contact.types';

const KINDS = Object.keys(contactSettings.kinds) as ContactKind[];

interface ContactFormProps {
  /** A prefix for the field ids, so an edit form and the add form can share a page. */
  idPrefix: string;
  initial: ContactInput;
  branches: { id: string; name: string }[];
  submitLabel: string;
  onSubmit: (values: ContactInput) => Promise<ActionResult<unknown>>;
  onDone: () => void;
  onCancel: () => void;
}

/**
 * One contact entry's fields, for adding and for changing. The value's hint changes with the kind,
 * and the map embed field appears only for a map. A refusal lands under the field it names.
 */
export function ContactForm({ idPrefix, initial, branches, submitLabel, onSubmit, onDone, onCancel }: ContactFormProps) {
  const text = contactSettings;
  const [values, setValues] = useState(initial);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string>();
  const [pending, startTransition] = useTransition();

  const set = <K extends keyof ContactInput>(field: K, value: ContactInput[K]) =>
    setValues((current) => ({ ...current, [field]: value }));
  const id = (field: string) => `${idPrefix}-${field}`;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrors({});
    setMessage(undefined);

    startTransition(async () => {
      try {
        // An embed belongs to a map only; switching the kind away drops it rather than being refused.
        const result = await onSubmit({ ...values, embed_url: values.kind === 'map' ? values.embed_url : '' });

        if (result.ok) {
          onDone();
          return;
        }

        if (result.error.code === 'version_conflict') {
          setMessage(text.conflict);
          return;
        }

        const byField = fieldErrors(result.error);
        setErrors(byField);
        if (Object.keys(byField).length === 0) setMessage(result.error.message);
      } catch {
        setMessage(text.unreachable);
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-4 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
      {message && <p role="alert" className="text-sm text-red-700">{message}</p>}

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.fields.kind} htmlFor={id('kind')} error={errors.kind}>
          <Select id={id('kind')} value={values.kind} onChange={(event) => set('kind', event.target.value as ContactKind)}>
            {KINDS.map((kind) => (
              <option key={kind} value={kind}>
                {text.kinds[kind].name}
              </option>
            ))}
          </Select>
        </Field>

        <Field label={text.fields.label} htmlFor={id('label')} hint={text.fields.labelHint} error={errors.label}>
          <Input id={id('label')} value={values.label} maxLength={100} required onChange={(event) => set('label', event.target.value)} />
        </Field>

        <Field label={text.fields.value} htmlFor={id('value')} hint={text.kinds[values.kind].valueHint} error={errors.value}>
          <Input id={id('value')} value={values.value} maxLength={500} required onChange={(event) => set('value', event.target.value)} />
        </Field>

        <Field label={text.fields.url} htmlFor={id('url')} hint={text.fields.urlHint} error={errors.url}>
          <Input id={id('url')} type="url" value={values.url} maxLength={1000} placeholder="https://" onChange={(event) => set('url', event.target.value)} />
        </Field>

        {values.kind === 'map' && (
          <div className="sm:col-span-2">
            <Field label={text.fields.embedUrl} htmlFor={id('embed')} hint={text.fields.embedUrlHint} error={errors.embed_url}>
              <Input id={id('embed')} value={values.embed_url} maxLength={1000} placeholder="https://www.google.com/maps/embed?pb=..." onChange={(event) => set('embed_url', event.target.value)} />
            </Field>
          </div>
        )}

        <Field label={text.fields.branch} htmlFor={id('branch')} error={errors.branch_id}>
          <Select id={id('branch')} value={values.branch_id} onChange={(event) => set('branch_id', event.target.value)}>
            <option value="">{text.clinicWide}</option>
            {branches.map((branch) => (
              <option key={branch.id} value={branch.id}>
                {branch.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field label={text.fields.order} htmlFor={id('order')} hint={text.fields.orderHint} error={errors.sort_order}>
          <Input
            id={id('order')}
            type="number"
            step={1}
            value={values.sort_order}
            className="max-w-32"
            onChange={(event) => set('sort_order', Number(event.target.value) || 0)}
          />
        </Field>
      </div>

      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={onCancel} disabled={pending}>
          {text.cancel}
        </Button>
        <Button type="submit" disabled={pending}>
          {pending ? text.saving : submitLabel}
        </Button>
      </div>
    </form>
  );
}

'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createBranchAction } from '../../actions/clinic-setup-actions';
import { clinicSettings } from '../../content/clinic-settings';
import { optionalNumber, parseCommaList } from '../../lib/parse-list';

const blank = { code: '', name: '', address: '', latitude: '', longitude: '', rooms: '' };

/** Opens a new branch. Closed behind a button, since it happens a handful of times in a clinic's life. */
export function AddBranchForm() {
  const text = clinicSettings.branches;
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
      () =>
        createBranchAction({
          code: values.code.trim().toLowerCase(),
          name: values.name,
          address: values.address.trim() || undefined,
          latitude: optionalNumber(values.latitude),
          longitude: optionalNumber(values.longitude),
          exam_rooms: parseCommaList(values.rooms),
        }),
      () => {
        setValues(blank);
        setOpen(false);
      },
    );
  }

  return (
    <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.code} htmlFor="branch-code" hint={text.codeHint} error={errors.code}>
          <Input id="branch-code" value={values.code} maxLength={32} required className="font-mono" onChange={(event) => set('code', event.target.value)} />
        </Field>

        <Field label={text.name} htmlFor="branch-name" error={errors.name}>
          <Input id="branch-name" value={values.name} maxLength={200} required onChange={(event) => set('name', event.target.value)} />
        </Field>

        <div className="sm:col-span-2">
          <Field label={text.address} htmlFor="branch-address" error={errors.address}>
            <Input id="branch-address" value={values.address} maxLength={500} onChange={(event) => set('address', event.target.value)} />
          </Field>
        </div>

        <Field label={text.latitude} htmlFor="branch-lat" hint={text.coordinatesHint} error={errors.latitude}>
          <Input id="branch-lat" inputMode="decimal" value={values.latitude} className="max-w-44" onChange={(event) => set('latitude', event.target.value)} />
        </Field>

        <Field label={text.longitude} htmlFor="branch-lng" error={errors.longitude}>
          <Input id="branch-lng" inputMode="decimal" value={values.longitude} className="max-w-44" onChange={(event) => set('longitude', event.target.value)} />
        </Field>

        <div className="sm:col-span-2">
          <Field label={text.examRooms} htmlFor="branch-rooms" hint={text.examRoomsHint} error={errors.exam_rooms}>
            <Input id="branch-rooms" value={values.rooms} onChange={(event) => set('rooms', event.target.value)} />
          </Field>
        </div>
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

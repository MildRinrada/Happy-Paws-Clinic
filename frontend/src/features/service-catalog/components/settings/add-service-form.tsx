'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input, Select } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createServiceAction } from '../../actions/service-actions';
import { serviceSettings } from '../../content/service-settings';
import { inputToSatang } from '../../lib/baht-input';
import type { ServiceCategory } from '../../types/managed-service.types';

const blank = { name: '', category_code: 'checkup', duration: '30', price: '', code: '', online: false };

/**
 * A new service. Off the public menu unless asked: a service on the website before its price
 * tiers and vets are set would be bookable half-configured.
 */
export function AddServiceForm({ categories }: { categories: ServiceCategory[] }) {
  const text = serviceSettings.details;
  const [open, setOpen] = useState(false);
  const [values, setValues] = useState(blank);
  const [badPrice, setBadPrice] = useState(false);
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  const set = <K extends keyof typeof blank>(field: K, value: (typeof blank)[K]) => setValues((current) => ({ ...current, [field]: value }));

  if (!open) {
    return <Button onClick={() => setOpen(true)}>{serviceSettings.add}</Button>;
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const price = inputToSatang(values.price);
    setBadPrice(price === null);
    if (price === null) return;

    submit(
      () =>
        createServiceAction({
          name: values.name,
          category_code: values.category_code,
          duration_minutes: Math.trunc(Number(values.duration) || 0),
          price_satang: price,
          code: values.code.trim().toLowerCase() || undefined,
          online_booking_enabled: values.online,
        }),
      () => {
        setValues(blank);
        setOpen(false);
      },
    );
  }

  return (
    <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-white p-4">
      <div className="grid gap-4 sm:grid-cols-3">
        <div className="sm:col-span-2">
          <Field label={text.name} htmlFor="new-service-name" error={errors.name}>
            <Input id="new-service-name" value={values.name} maxLength={200} required onChange={(event) => set('name', event.target.value)} />
          </Field>
        </div>

        <Field label={text.category} htmlFor="new-service-category" error={errors.category_code}>
          <Select id="new-service-category" value={values.category_code} onChange={(event) => set('category_code', event.target.value)}>
            {categories.map((category) => (
              <option key={category.code} value={category.code}>
                {category.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field label={text.duration} htmlFor="new-service-duration" error={errors.duration_minutes}>
          <Input
            id="new-service-duration"
            type="number"
            min={5}
            max={480}
            step={5}
            value={values.duration}
            required
            className="max-w-32"
            onChange={(event) => set('duration', event.target.value)}
          />
        </Field>

        <Field label={text.price} htmlFor="new-service-price" error={badPrice ? serviceSettings.pricing.badPrice : errors.price_satang}>
          <Input id="new-service-price" inputMode="decimal" value={values.price} required className="max-w-40" onChange={(event) => set('price', event.target.value)} />
        </Field>

        <Field label={text.code} htmlFor="new-service-code" hint={text.codeHint} error={errors.code}>
          <Input id="new-service-code" value={values.code} maxLength={32} className="font-mono" onChange={(event) => set('code', event.target.value)} />
        </Field>
      </div>

      <Checkbox label={text.onlineNow} hint={text.onlineNowHint} checked={values.online} onChange={(event) => set('online', event.target.checked)} />

      <FormFooter pending={pending} message={message} onCancel={() => setOpen(false)} submitLabel={formMessages.add} pendingLabel={formMessages.adding} />
    </form>
  );
}

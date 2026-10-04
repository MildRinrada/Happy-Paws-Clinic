'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import type { TemplateProcedure } from '@/features/prescriptions/types/treatment-template.types';
import { parseBahtToSatang } from '@/lib/format/money';
import { addTreatmentAction } from '../../actions/consultation-actions';
import { consultationPage } from '../../content/consultation-page';
import { usePartSubmit } from '../../hooks/use-part-submit';
import { TemplatePicker } from './template-picker';

/**
 * Adds a procedure. Each one goes on the bill when the case closes, so the request carries an
 * idempotency key: a double press must not charge twice. The key is renewed after each line saved.
 *
 * A treatment template can fill the name, quantity and note. It never submits, and the price is
 * left to the vet: templates hold no price.
 */
export function TreatmentForm({ consultationId }: { consultationId: string }) {
  const text = consultationPage.treatments;
  const [name, setName] = useState('');
  const [quantity, setQuantity] = useState('1');
  const [price, setPrice] = useState('');
  const [note, setNote] = useState('');
  const [priceError, setPriceError] = useState<string>();
  const [key, setKey] = useState(() => crypto.randomUUID());
  const { errors, message, pending, submit } = usePartSubmit();

  function applyTemplate(procedure: TemplateProcedure) {
    setName(procedure.name);
    setQuantity(String(procedure.quantity));
    setNote(procedure.note ?? '');
    // The price is the one field a template cannot fill.
    document.getElementById('treatment-price')?.focus();
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const unitPriceSatang = parseBahtToSatang(price);
    if (unitPriceSatang === null) {
      setPriceError(text.badPrice);
      return;
    }
    setPriceError(undefined);

    submit(
      () => addTreatmentAction({ consultationId, name, quantity: Number(quantity), unitPriceSatang, note, idempotencyKey: key }),
      () => {
        setName('');
        setQuantity('1');
        setPrice('');
        setNote('');
        setKey(crypto.randomUUID());
      },
    );
  }

  return (
    <form onSubmit={save} className="space-y-3 border-t border-ink-100 pt-4">
      {message && <p role="alert" className="text-sm text-red-700">{message}</p>}
      <TemplatePicker onUse={applyTemplate} />
      <div className="grid gap-3 sm:grid-cols-[1fr_6rem_9rem]">
        <Field label={text.fields.name} htmlFor="treatment-name" error={errors.name}>
          <Input id="treatment-name" value={name} maxLength={200} required onChange={(event) => setName(event.target.value)} />
        </Field>
        <Field label={text.fields.quantity} htmlFor="treatment-quantity" error={errors.quantity}>
          <Input id="treatment-quantity" type="number" min={1} step={1} value={quantity} required onChange={(event) => setQuantity(event.target.value)} />
        </Field>
        <Field label={text.fields.price} htmlFor="treatment-price" error={priceError ?? errors.unit_price_satang}>
          <Input id="treatment-price" inputMode="decimal" value={price} required onChange={(event) => setPrice(event.target.value)} />
        </Field>
      </div>
      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-0 flex-1">
          <Field label={text.fields.note} htmlFor="treatment-note">
            <Input id="treatment-note" value={note} maxLength={500} onChange={(event) => setNote(event.target.value)} />
          </Field>
        </div>
        <Button type="submit" disabled={pending}>
          {pending ? consultationPage.saving : consultationPage.add}
        </Button>
      </div>
      <p className="text-xs text-ink-500">{text.hint}</p>
    </form>
  );
}

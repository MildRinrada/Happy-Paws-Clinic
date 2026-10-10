'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { parseBahtToSatang } from '@/lib/format/money';
import { updateItemAction } from '../actions/manage-inventory-actions';
import { itemEditText } from '../content/item-edit';
import type { InventoryItem, ItemChanges } from '../types/inventory.types';

type Draft = { name: string; unit: string; price: string; reorder: string };
type DraftField = keyof Draft;

const draftOf = (item: InventoryItem): Draft => ({
  name: item.name,
  unit: item.unit,
  price: String(item.unit_price_satang / 100),
  reorder: String(item.reorder_level),
});

/** The changed fields only, or the fields that do not parse. */
function changesFrom(item: InventoryItem, draft: Draft): { changes?: ItemChanges; bad: DraftField[] } {
  const price = parseBahtToSatang(draft.price);
  const reorder = /^\d+$/.test(draft.reorder.trim()) ? Number(draft.reorder.trim()) : null;
  const bad: DraftField[] = [];

  if (!draft.name.trim()) bad.push('name');
  if (!draft.unit.trim()) bad.push('unit');
  if (price === null) bad.push('price');
  if (reorder === null) bad.push('reorder');
  if (bad.length > 0 || price === null || reorder === null) return { bad };

  const changes: ItemChanges = {};
  if (draft.name.trim() !== item.name) changes.name = draft.name.trim();
  if (draft.unit.trim() !== item.unit) changes.unit = draft.unit.trim();
  if (price !== item.unit_price_satang) changes.unit_price_satang = price;
  if (reorder !== item.reorder_level) changes.reorder_level = reorder;

  return { changes, bad };
}

/**
 * Name, unit, price and reorder level of one item, sending only what changed with the version read
 * as `If-Match`. docs/contract-extensions.md § 25.
 */
export function ItemEditForm({ item, onDone }: { item: InventoryItem; onDone: () => void }) {
  const text = itemEditText;
  const id = useId();
  const [draft, setDraft] = useState<Draft>(() => draftOf(item));
  const [bad, setBad] = useState<DraftField[]>([]);
  const [nothing, setNothing] = useState(false);
  const { errors, message, pending, submit, touch } = useFormSubmit(formMessages);

  const fields: { field: DraftField; api: string; label: string; hint?: string; error: string; mode?: 'decimal' | 'numeric' }[] = [
    { field: 'name', api: 'name', label: text.name, hint: text.nameHint, error: text.required },
    { field: 'unit', api: 'unit', label: text.unit, hint: text.unitHint, error: text.required },
    { field: 'price', api: 'unit_price_satang', label: text.price, hint: text.priceHint, error: text.amount, mode: 'decimal' },
    { field: 'reorder', api: 'reorder_level', label: text.reorder, error: text.wholeNumber, mode: 'numeric' },
  ];

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const { changes, bad: wrong } = changesFrom(item, draft);
    setBad(wrong);
    setNothing(false);
    if (!changes) return;
    if (Object.keys(changes).length === 0) return setNothing(true);

    submit(() => updateItemAction({ id: item.id, version: item.version, changes }), onDone);
  }

  return (
    <form onSubmit={save} noValidate className="mt-2 w-72 space-y-3 rounded-lg border border-ink-200 bg-ink-50 p-3">
      {fields.map(({ field, api, label, hint, error, mode }) => (
        <Field key={field} label={label} htmlFor={`${id}-${field}`} hint={hint} error={bad.includes(field) ? error : errors[api]}>
          <Input
            id={`${id}-${field}`}
            inputMode={mode}
            value={draft[field]}
            className="h-9"
            aria-invalid={bad.includes(field) || Boolean(errors[api])}
            onChange={(event) => {
              touch();
              setDraft((current) => ({ ...current, [field]: event.target.value }));
            }}
          />
        </Field>
      ))}
      {nothing && <p className="text-xs text-ink-600">{text.nothing}</p>}
      <div className="flex flex-wrap items-center justify-end gap-2">
        <FormStatus message={message} savedText="" />
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? text.submitting : text.submit}
        </Button>
      </div>
    </form>
  );
}

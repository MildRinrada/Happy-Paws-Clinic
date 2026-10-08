'use client';

import { Field, Input, Select } from '@/components/ui/field';
import { kindLabels } from '../content/inventory-labels';
import { itemFields } from '../content/item-fields';
import type { NewItemDraft } from '../lib/new-item-draft';
import type { ItemKind } from '../types/inventory.types';

interface Props {
  idPrefix: string;
  draft: NewItemDraft;
  onChange: (draft: NewItemDraft) => void;
  /** Refusals keyed by the API's field names, already narrowed to this item (`name`, `unit`, …). */
  errors?: Record<string, string | undefined>;
}

const KINDS: ItemKind[] = ['medicine', 'supply'];

/** A drug or supply the catalogue does not have yet, created by the receipt or order that names it. */
export function NewItemFields({ idPrefix, draft, onChange, errors = {} }: Props) {
  const set = (patch: Partial<NewItemDraft>) => onChange({ ...draft, ...patch });

  return (
    <div className="grid gap-3 rounded-lg border border-dashed border-ink-200 p-3 sm:grid-cols-2">
      <div className="sm:col-span-2">
        <Field label={itemFields.name} htmlFor={`${idPrefix}-name`} hint={itemFields.nameHint} error={errors.name}>
          <Input id={`${idPrefix}-name`} value={draft.name} maxLength={200} onChange={(event) => set({ name: event.target.value })} />
        </Field>
      </div>
      <Field label={itemFields.kind} htmlFor={`${idPrefix}-kind`}>
        <Select id={`${idPrefix}-kind`} value={draft.kind} onChange={(event) => set({ kind: event.target.value as ItemKind })}>
          {KINDS.map((kind) => (
            <option key={kind} value={kind}>
              {kindLabels[kind]}
            </option>
          ))}
        </Select>
      </Field>
      <Field label={itemFields.unit} htmlFor={`${idPrefix}-unit`} error={errors.unit}>
        <Input
          id={`${idPrefix}-unit`}
          value={draft.unit}
          maxLength={40}
          placeholder={itemFields.unitPlaceholder}
          onChange={(event) => set({ unit: event.target.value })}
        />
      </Field>
      <Field label={itemFields.price} htmlFor={`${idPrefix}-price`} hint={itemFields.priceHint} error={errors.unit_price_satang}>
        <Input id={`${idPrefix}-price`} inputMode="decimal" value={draft.price} onChange={(event) => set({ price: event.target.value })} />
      </Field>
      <Field label={itemFields.reorder} htmlFor={`${idPrefix}-reorder`} hint={itemFields.reorderHint} error={errors.reorder_level}>
        <Input
          id={`${idPrefix}-reorder`}
          type="number"
          min={0}
          value={draft.reorder}
          onChange={(event) => set({ reorder: event.target.value })}
        />
      </Field>
    </div>
  );
}

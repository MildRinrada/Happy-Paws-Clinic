'use client';

import { Field, Select } from '@/components/ui/field';
import { itemFields } from '../content/item-fields';
import { NEW_ITEM } from '../lib/new-item-draft';
import type { InventoryItem } from '../types/inventory.types';

interface Props {
  id: string;
  items: InventoryItem[];
  value: string;
  onChange: (value: string) => void;
  /** Offers "a new item" — for receipts and orders, which is how an item first enters the catalogue. */
  allowNew?: boolean;
  error?: string;
}

/** Choose an item from the catalogue, or a new one. Shows each item's usable units at the branch in view. */
export function ItemPicker({ id, items, value, onChange, allowNew = false, error }: Props) {
  return (
    <Field label={itemFields.item} htmlFor={id} error={error}>
      <Select id={id} value={value} onChange={(event) => onChange(event.target.value)} aria-invalid={Boolean(error)}>
        <option value="">{itemFields.choose}</option>
        {allowNew && <option value={NEW_ITEM}>{itemFields.newItem}</option>}
        {items.map((item) => (
          <option key={item.id} value={item.id}>
            {itemFields.option(item.name, item.available, item.unit)}
          </option>
        ))}
      </Select>
    </Field>
  );
}

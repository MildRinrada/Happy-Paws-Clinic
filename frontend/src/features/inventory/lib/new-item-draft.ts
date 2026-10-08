import { parseBahtToSatang } from '@/lib/format/money';
import type { ItemKind, NewItemInput } from '../types/inventory.types';

/** What the "new item" fields hold while being typed: the price as the person typed it, in baht. */
export interface NewItemDraft {
  name: string;
  kind: ItemKind;
  unit: string;
  price: string;
  reorder: string;
}

export const emptyNewItem: NewItemDraft = { name: '', kind: 'medicine', unit: '', price: '', reorder: '0' };

/** The picker's value for "a new item". Never an item id: those start with `itm_`. */
export const NEW_ITEM = 'new';

/** The draft as the API wants it, or null while a field is missing or not a number. */
export function toNewItem(draft: NewItemDraft): NewItemInput | null {
  const price = parseBahtToSatang(draft.price);
  const reorder = Number(draft.reorder || '0');

  if (!draft.name.trim() || !draft.unit.trim() || price === null || !Number.isInteger(reorder) || reorder < 0) {
    return null;
  }

  return { name: draft.name.trim(), kind: draft.kind, unit: draft.unit.trim(), unit_price_satang: price, reorder_level: reorder };
}

/** A whole number of units, at least `min`, or null. */
export function parseCount(value: string, min = 1): number | null {
  const count = Number(value);

  return value.trim() !== '' && Number.isInteger(count) && count >= min ? count : null;
}

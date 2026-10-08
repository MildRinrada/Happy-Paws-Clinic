import { parseBahtToSatang } from '@/lib/format/money';
import type { PurchaseOrderLineInput } from '../types/inventory.types';
import { emptyNewItem, NEW_ITEM, parseCount, toNewItem, type NewItemDraft } from './new-item-draft';

/** One order line as typed. `key` keeps React's rows stable as lines are added and removed. */
export interface OrderLineDraft {
  key: string;
  itemValue: string;
  newItem: NewItemDraft;
  quantity: string;
  unitCost: string;
}

export const emptyLine = (): OrderLineDraft => ({
  key: crypto.randomUUID(),
  itemValue: '',
  newItem: emptyNewItem,
  quantity: '',
  unitCost: '',
});

/** The line as the API wants it, or null while it is incomplete. */
export function toOrderLine(line: OrderLineDraft): PurchaseOrderLineInput | null {
  const quantity = parseCount(line.quantity);
  const cost = parseBahtToSatang(line.unitCost);

  if (!line.itemValue || quantity === null || cost === null) {
    return null;
  }

  if (line.itemValue !== NEW_ITEM) {
    return { item_id: line.itemValue, quantity, unit_cost_satang: cost };
  }

  const newItem = toNewItem(line.newItem);

  return newItem ? { new_item: newItem, quantity, unit_cost_satang: cost } : null;
}

/** What the lines typed so far cost, in satang. Incomplete lines count as nothing. */
export function draftTotal(lines: OrderLineDraft[]): number {
  return lines.reduce((sum, line) => {
    const input = toOrderLine(line);
    return sum + (input ? input.quantity * input.unit_cost_satang : 0);
  }, 0);
}

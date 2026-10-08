'use client';

import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { purchaseOrderPage } from '../content/purchase-order-page';
import { errorsUnder } from '../lib/errors-under';
import { NEW_ITEM } from '../lib/new-item-draft';
import type { OrderLineDraft } from '../lib/order-draft';
import type { InventoryItem } from '../types/inventory.types';
import { ItemPicker } from './item-picker';
import { NewItemFields } from './new-item-fields';

interface Props {
  index: number;
  line: OrderLineDraft;
  items: InventoryItem[];
  onChange: (line: OrderLineDraft) => void;
  onRemove?: () => void;
  /** Refusals for this line, with the `lines[n].` prefix taken off. */
  errors: Record<string, string>;
}

/** One item on the order: which (or a new one), how many, at what cost. */
export function OrderLineRow({ index, line, items, onChange, onRemove, errors }: Props) {
  const text = purchaseOrderPage;
  const id = `order-line-${line.key}`;
  const set = (patch: Partial<OrderLineDraft>) => onChange({ ...line, ...patch });

  return (
    <fieldset className="space-y-3 rounded-lg border border-ink-200 p-3">
      <div className="flex items-center justify-between">
        <legend className="text-sm font-medium text-ink-800">{text.lineHeading(index + 1)}</legend>
        {onRemove && (
          <Button size="sm" variant="ghost" onClick={onRemove}>
            {text.removeLine}
          </Button>
        )}
      </div>
      <div className="grid gap-3 sm:grid-cols-4">
        <div className="sm:col-span-2">
          <ItemPicker id={`${id}-item`} items={items} value={line.itemValue} allowNew error={errors.item_id} onChange={(itemValue) => set({ itemValue })} />
        </div>
        <Field label={text.quantity} htmlFor={`${id}-quantity`} error={errors.quantity}>
          <Input id={`${id}-quantity`} type="number" min={1} value={line.quantity} onChange={(event) => set({ quantity: event.target.value })} />
        </Field>
        <Field label={text.unitCost} htmlFor={`${id}-cost`} error={errors.unit_cost_satang}>
          <Input id={`${id}-cost`} inputMode="decimal" value={line.unitCost} onChange={(event) => set({ unitCost: event.target.value })} />
        </Field>
      </div>
      {line.itemValue === NEW_ITEM && (
        <NewItemFields
          idPrefix={`${id}-new`}
          draft={line.newItem}
          onChange={(newItem) => set({ newItem })}
          errors={errorsUnder(errors, 'new_item.')}
        />
      )}
    </fieldset>
  );
}

'use client';

import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { adjustStockAction } from '../actions/inventory-actions';
import { adjustmentPage } from '../content/adjustment-page';
import { inventoryArea } from '../content/inventory-tabs';
import { itemFields } from '../content/item-fields';
import { parseCount } from '../lib/new-item-draft';
import { stockAt } from '../lib/stock-at';
import type { InventoryItem, StockChange } from '../types/inventory.types';
import { ItemPicker } from './item-picker';
import { LotSelect } from './lot-select';

interface Props {
  branchId?: string;
  items: InventoryItem[];
}

/**
 * "ปรับยอดหลังนับสต๊อก": what was counted, on which lot, and why. The difference is the backend's to
 * work out, against the balance at the moment it saves, so a dispense in between is not undone.
 */
export function AdjustmentForm({ branchId, items }: Props) {
  const text = adjustmentPage;
  const [itemId, setItemId] = useState('');
  const [lotId, setLotId] = useState('');
  const [counted, setCounted] = useState('');
  const [reason, setReason] = useState('');
  const [local, setLocal] = useState<string>();
  const [result, setResult] = useState<StockChange>();
  const { errors, message, pending, submit, touch } = useFormSubmit({ conflict: inventoryArea.conflict, unreachable: inventoryArea.unreachable });

  const item = items.find((candidate) => candidate.id === itemId);
  const lots = item ? stockAt(item, branchId).lots : [];
  const chosenLot = lots.find((lot) => lot.id === lotId);
  const booked = chosenLot ? chosenLot.quantity_on_hand : lots.length === 1 ? lots[0]!.quantity_on_hand : undefined;

  function change(apply: () => void) {
    touch();
    setLocal(undefined);
    apply();
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const count = parseCount(counted, 0);

    if (!itemId) return setLocal(itemFields.choose);
    if (count === null) return setLocal(text.badCount);
    if (!reason.trim()) return setLocal(text.needReason);
    if (lots.length > 1 && !lotId) return setLocal(text.lotRequired);

    setResult(undefined);
    submit(
      () => adjustStockAction({ branch_id: branchId, item_id: itemId, lot_id: lotId || undefined, counted_quantity: count, reason: reason.trim() }),
      (done) => {
        setResult(done);
        setCounted('');
        setReason('');
      },
    );
  }

  const difference = result?.movements[0]?.quantity ?? 0;

  return (
    <form onSubmit={save} className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-2">
        <ItemPicker id="count-item" items={items} value={itemId} error={errors.item_id} onChange={(value) => change(() => { setItemId(value); setLotId(''); })} />
        <LotSelect
          id="count-lot"
          label={text.lot}
          lots={lots}
          value={lotId}
          emptyLabel={text.onlyLot}
          error={errors.lot_id}
          onChange={(value) => change(() => setLotId(value))}
        />
      </div>
      <div className="grid gap-3 sm:grid-cols-3">
        <Field label={text.counted} htmlFor="count-quantity" hint={booked === undefined ? undefined : text.booked(booked)} error={errors.counted_quantity}>
          <Input id="count-quantity" type="number" min={0} value={counted} onChange={(event) => change(() => setCounted(event.target.value))} />
        </Field>
        <div className="sm:col-span-2">
          <Field label={text.reason} htmlFor="count-reason" error={errors.reason}>
            <Textarea id="count-reason" rows={1} maxLength={500} value={reason} placeholder={text.reasonPlaceholder} onChange={(event) => change(() => setReason(event.target.value))} />
          </Field>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? text.saving : text.submit}
        </Button>
        <FormStatus message={local ?? message} savedText="" />
      </div>

      {result && <Alert tone="success">{text.result(difference, stockAt(result.item, branchId).available, result.item.unit)}</Alert>}
    </form>
  );
}

'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { formatBaht } from '@/lib/format/money';
import { createPurchaseOrderAction } from '../actions/inventory-actions';
import { inventoryArea } from '../content/inventory-tabs';
import { purchaseOrderPage } from '../content/purchase-order-page';
import { errorsUnder } from '../lib/errors-under';
import { draftTotal, emptyLine, toOrderLine, type OrderLineDraft } from '../lib/order-draft';
import type { InventoryItem, PurchaseOrder, PurchaseOrderLineInput } from '../types/inventory.types';
import { OrderLineRow } from './order-line-row';
import { PurchaseOrderSummary } from './purchase-order-summary';

interface Props {
  branchId?: string;
  items: InventoryItem[];
}

/**
 * "สั่งซื้อจากซัพพลายเออร์": the supplier and the lines. Raising an order changes no stock; the goods
 * are received against it later. One idempotency key per order, kept across retries.
 */
export function PurchaseOrderForm({ branchId, items }: Props) {
  const text = purchaseOrderPage;
  const [supplier, setSupplier] = useState('');
  const [expectedOn, setExpectedOn] = useState('');
  const [note, setNote] = useState('');
  const [lines, setLines] = useState<OrderLineDraft[]>(() => [emptyLine()]);
  const [key, setKey] = useState(() => crypto.randomUUID());
  const [local, setLocal] = useState<string>();
  const [order, setOrder] = useState<PurchaseOrder>();
  const { errors, message, pending, submit, touch } = useFormSubmit({ conflict: inventoryArea.conflict, unreachable: inventoryArea.unreachable });

  function edit(apply: () => void) {
    touch();
    setLocal(undefined);
    apply();
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const inputs = lines.map(toOrderLine);

    if (!supplier.trim() || inputs.some((line) => line === null)) {
      setLocal(text.incomplete);
      return;
    }

    submit(
      () =>
        createPurchaseOrderAction({
          idempotencyKey: key,
          input: {
            branch_id: branchId,
            supplier_name: supplier.trim(),
            expected_on: expectedOn || undefined,
            note: note.trim() || undefined,
            lines: inputs as PurchaseOrderLineInput[],
          },
        }),
      setOrder,
    );
  }

  function another() {
    setOrder(undefined);
    setSupplier('');
    setExpectedOn('');
    setNote('');
    setLines([emptyLine()]);
    setKey(crypto.randomUUID());
  }

  if (order) {
    return <PurchaseOrderSummary order={order} onAnother={another} />;
  }

  return (
    <form onSubmit={save} className="space-y-4">
      <div className="grid gap-3 sm:grid-cols-3">
        <div className="sm:col-span-2">
          <Field label={text.supplier} htmlFor="order-supplier" error={errors.supplier_name}>
            <Input id="order-supplier" value={supplier} maxLength={200} onChange={(event) => edit(() => setSupplier(event.target.value))} />
          </Field>
        </div>
        <Field label={text.expectedOn} htmlFor="order-expected" error={errors.expected_on}>
          <Input id="order-expected" type="date" value={expectedOn} onChange={(event) => edit(() => setExpectedOn(event.target.value))} />
        </Field>
      </div>

      <div className="space-y-3">
        <h2 className="text-sm font-medium text-ink-800">{text.lines}</h2>
        {errors.lines && <p role="alert" className="text-sm text-red-700">{errors.lines}</p>}
        {lines.map((line, index) => (
          <OrderLineRow
            key={line.key}
            index={index}
            line={line}
            items={items}
            errors={errorsUnder(errors, `lines[${index}].`)}
            onChange={(next) => edit(() => setLines((current) => current.map((candidate) => (candidate.key === line.key ? next : candidate))))}
            onRemove={lines.length > 1 ? () => edit(() => setLines((current) => current.filter((candidate) => candidate.key !== line.key))) : undefined}
          />
        ))}
        {lines.length < 50 && (
          <Button size="sm" variant="secondary" onClick={() => edit(() => setLines((current) => [...current, emptyLine()]))}>
            {text.addLine}
          </Button>
        )}
      </div>

      <Field label={text.note} htmlFor="order-note" error={errors.note}>
        <Textarea id="order-note" rows={2} maxLength={1000} value={note} onChange={(event) => edit(() => setNote(event.target.value))} />
      </Field>

      <div className="flex flex-wrap items-center gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? text.saving : text.submit}
        </Button>
        <span className="text-sm font-medium tabular-nums text-ink-700">{text.total(formatBaht(draftTotal(lines)))}</span>
        <FormStatus message={local ?? message} savedText="" />
      </div>
    </form>
  );
}

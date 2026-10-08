'use client';

import { Field, Input, Select } from '@/components/ui/field';
import { movementPage } from '../content/movement-page';
import type { ReceiveDraft } from '../lib/receive-draft';
import type { OpenOrder } from '../types/inventory.types';

interface Props {
  draft: ReceiveDraft;
  onChange: (draft: ReceiveDraft) => void;
  /** Orders for this item at this branch with goods still to come — "รับของบางส่วนได้". */
  orders: OpenOrder[];
  errors: Record<string, string>;
}

/** What only a receipt has: the lot, its expiry, what it cost, and the order it arrived against. */
export function ReceiveFields({ draft, onChange, orders, errors }: Props) {
  const text = movementPage;
  const set = (patch: Partial<ReceiveDraft>) => onChange({ ...draft, ...patch });

  return (
    <div className="grid gap-3 sm:grid-cols-2">
      <Field label={text.lotNumber} htmlFor="move-lot-number" error={errors.lot_number}>
        <Input id="move-lot-number" value={draft.lotNumber} maxLength={60} onChange={(event) => set({ lotNumber: event.target.value })} />
      </Field>
      <Field label={text.expiresOn} htmlFor="move-expires" error={errors.expires_on}>
        <Input id="move-expires" type="date" value={draft.expiresOn} onChange={(event) => set({ expiresOn: event.target.value })} />
      </Field>
      <Field label={text.unitCost} htmlFor="move-cost" hint={text.unitCostHint} error={errors.unit_cost_satang}>
        <Input id="move-cost" inputMode="decimal" value={draft.unitCost} onChange={(event) => set({ unitCost: event.target.value })} />
      </Field>
      {orders.length > 0 && (
        <Field label={text.order} htmlFor="move-order" error={errors.purchase_order_id}>
          <Select id="move-order" value={draft.orderId} onChange={(event) => set({ orderId: event.target.value })}>
            <option value="">{text.noOrder}</option>
            {orders.map((order) => (
              <option key={order.purchase_order_id} value={order.purchase_order_id}>
                {text.orderOption(order.number, order.outstanding)}
              </option>
            ))}
          </Select>
        </Field>
      )}
    </div>
  );
}

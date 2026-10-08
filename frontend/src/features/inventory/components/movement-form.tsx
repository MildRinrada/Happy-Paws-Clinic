'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Select, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { recordMovementAction } from '../actions/inventory-actions';
import { inventoryArea } from '../content/inventory-tabs';
import { itemFields } from '../content/item-fields';
import { movementPage } from '../content/movement-page';
import { buildMovement, type MovementDraft } from '../lib/build-movement';
import { errorsUnder } from '../lib/errors-under';
import { emptyNewItem, NEW_ITEM } from '../lib/new-item-draft';
import { emptyReceive } from '../lib/receive-draft';
import { stockAt } from '../lib/stock-at';
import type { InventoryItem, MovementInput, StockChange } from '../types/inventory.types';
import { ItemPicker } from './item-picker';
import { LotSelect } from './lot-select';
import { MovementResult } from './movement-result';
import { NewItemFields } from './new-item-fields';
import { ReceiveFields } from './receive-fields';

interface Props {
  branchId?: string;
  items: InventoryItem[];
  /** Branches stock may be sent to. */
  destinations: { id: string; name: string }[];
  /** Without `inventory:read` there is no catalogue to choose from: only a new item can be received. */
  canRead: boolean;
}

const TYPES: MovementInput['type'][] = ['receive', 'issue', 'transfer'];

const start = (branchId?: string): MovementDraft => ({
  type: 'receive',
  branchId,
  itemValue: '',
  newItem: emptyNewItem,
  quantity: '',
  receive: emptyReceive,
  lotId: '',
  toBranchId: '',
  reference: '',
  note: '',
});

/**
 * "รับเข้า จ่ายออก โอนสาขา" in one form. The idempotency key is minted once per movement and kept
 * for every retry of it, so a double press cannot receive one delivery twice.
 */
export function MovementForm({ branchId, items, destinations, canRead }: Props) {
  const text = movementPage;
  const [draft, setDraft] = useState<MovementDraft>(() => start(branchId));
  const [key, setKey] = useState(() => crypto.randomUUID());
  const [local, setLocal] = useState<string>();
  const [result, setResult] = useState<StockChange>();
  const { errors, message, pending, submit, touch } = useFormSubmit({ conflict: inventoryArea.conflict, unreachable: inventoryArea.unreachable });

  const set = (patch: Partial<MovementDraft>) => {
    touch();
    setLocal(undefined);
    setDraft((current) => ({ ...current, ...patch }));
  };

  const item = items.find((candidate) => candidate.id === draft.itemValue);
  const stock = item ? stockAt(item, branchId) : undefined;

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const built = buildMovement(draft);

    if ('error' in built) {
      setLocal(built.error);
      return;
    }

    setResult(undefined);
    submit(
      () => recordMovementAction({ idempotencyKey: key, input: built.input }),
      (change) => {
        setResult(change);
        setKey(crypto.randomUUID());
        setDraft({ ...start(branchId), type: draft.type, itemValue: draft.itemValue === NEW_ITEM ? change.item.id : draft.itemValue });
      },
    );
  }

  return (
    <form onSubmit={save} className="space-y-4">
      {!canRead && <p className="text-sm text-ink-600">{itemFields.noRead}</p>}

      <div className="grid gap-3 sm:grid-cols-3">
        <Field label={text.type} htmlFor="move-type">
          <Select id="move-type" value={draft.type} onChange={(event) => set({ type: event.target.value as MovementInput['type'], lotId: '' })}>
            {TYPES.filter((type) => canRead || type === 'receive').map((type) => (
              <option key={type} value={type}>
                {text.types[type]}
              </option>
            ))}
          </Select>
        </Field>
        <div className="sm:col-span-2">
          <ItemPicker
            id="move-item"
            items={items}
            value={draft.itemValue}
            allowNew={draft.type === 'receive'}
            error={errors.item_id}
            onChange={(itemValue) => set({ itemValue, lotId: '', receive: { ...draft.receive, orderId: '' } })}
          />
        </div>
      </div>

      {draft.type === 'receive' && draft.itemValue === NEW_ITEM && (
        <NewItemFields idPrefix="move-new" draft={draft.newItem} errors={errorsUnder(errors, 'new_item.')} onChange={(newItem) => set({ newItem })} />
      )}

      <div className="grid gap-3 sm:grid-cols-3">
        <Field label={text.quantity} htmlFor="move-quantity" error={errors.quantity}>
          <Input id="move-quantity" type="number" min={1} value={draft.quantity} onChange={(event) => set({ quantity: event.target.value })} />
        </Field>
        {draft.type !== 'receive' && stock && (
          <LotSelect id="move-lot" label={text.lot} lots={stock.lots} value={draft.lotId} emptyLabel={text.autoLot} error={errors.lot_id} onChange={(lotId) => set({ lotId })} />
        )}
        {draft.type === 'transfer' && (
          <Field label={text.toBranch} htmlFor="move-to" error={errors.to_branch_id}>
            <Select id="move-to" value={draft.toBranchId} onChange={(event) => set({ toBranchId: event.target.value })}>
              <option value="">{text.chooseBranch}</option>
              {destinations.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </Select>
          </Field>
        )}
      </div>

      {draft.type === 'receive' && (
        <ReceiveFields draft={draft.receive} orders={stock?.open_orders ?? []} errors={errors} onChange={(receive) => set({ receive })} />
      )}

      <div className="grid gap-3 sm:grid-cols-2">
        <Field label={text.reference} htmlFor="move-reference" error={errors.reference}>
          <Input id="move-reference" value={draft.reference} maxLength={120} placeholder={text.referencePlaceholder} onChange={(event) => set({ reference: event.target.value })} />
        </Field>
        <Field label={text.note} htmlFor="move-note" error={errors.note}>
          <Textarea id="move-note" rows={1} value={draft.note} maxLength={500} onChange={(event) => set({ note: event.target.value })} />
        </Field>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? text.saving : text.submit}
        </Button>
        <FormStatus message={local ?? message} savedText="" />
      </div>

      {result && <MovementResult change={result} branchId={branchId} />}
    </form>
  );
}

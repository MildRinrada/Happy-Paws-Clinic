import { parseBahtToSatang } from '@/lib/format/money';
import { itemFields } from '../content/item-fields';
import { movementPage } from '../content/movement-page';
import type { MovementInput } from '../types/inventory.types';
import { NEW_ITEM, parseCount, toNewItem, type NewItemDraft } from './new-item-draft';
import type { ReceiveDraft } from './receive-draft';

export interface MovementDraft {
  type: MovementInput['type'];
  branchId?: string;
  itemValue: string;
  newItem: NewItemDraft;
  quantity: string;
  receive: ReceiveDraft;
  lotId: string;
  toBranchId: string;
  reference: string;
  note: string;
}

const blank = (value: string) => (value.trim() ? value.trim() : undefined);

/** The form as the API wants it, or the first thing still wrong with it, said in the form's words. */
export function buildMovement(draft: MovementDraft): { input: MovementInput } | { error: string } {
  const quantity = parseCount(draft.quantity);

  if (quantity === null) {
    return { error: movementPage.badQuantity };
  }

  if (!draft.itemValue) {
    return { error: itemFields.choose };
  }

  const isNew = draft.type === 'receive' && draft.itemValue === NEW_ITEM;
  const newItem = isNew ? toNewItem(draft.newItem) : undefined;

  if (isNew && !newItem) {
    return { error: itemFields.badAmount };
  }

  const input: MovementInput = {
    type: draft.type,
    branch_id: draft.branchId,
    item_id: isNew ? undefined : draft.itemValue,
    new_item: newItem ?? undefined,
    quantity,
    reference: blank(draft.reference),
    note: blank(draft.note),
  };

  if (draft.type === 'receive') {
    const cost = draft.receive.unitCost.trim() ? parseBahtToSatang(draft.receive.unitCost) : undefined;

    if (cost === null) {
      return { error: itemFields.badAmount };
    }

    return {
      input: {
        ...input,
        lot_number: blank(draft.receive.lotNumber),
        expires_on: blank(draft.receive.expiresOn),
        unit_cost_satang: cost,
        purchase_order_id: blank(draft.receive.orderId),
      },
    };
  }

  return {
    input: {
      ...input,
      lot_id: blank(draft.lotId),
      to_branch_id: draft.type === 'transfer' ? blank(draft.toBranchId) : undefined,
    },
  };
}

'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { ConfirmButton } from '@/features/integrations/components/confirm-button';
import { updateItemAction } from '../actions/manage-inventory-actions';
import { itemEditText } from '../content/item-edit';
import type { InventoryItem } from '../types/inventory.types';
import { ItemEditForm } from './item-edit-form';

/** The edit toggle and the retire (or bring back) button of one row. */
export function ItemManageCell({ item }: { item: InventoryItem }) {
  const text = itemEditText;
  const [open, setOpen] = useState(false);
  const toggle = item.active ? text.retire : text.reactivate;

  return (
    <div className="flex flex-col items-start gap-1.5">
      <Button size="sm" variant="secondary" aria-expanded={open} onClick={() => setOpen((current) => !current)}>
        {open ? text.close : text.edit}
      </Button>
      {open && <ItemEditForm item={item} onDone={() => setOpen(false)} />}
      <ConfirmButton
        label={toggle.button}
        confirmText={toggle.confirm}
        confirmLabel={toggle.confirmButton}
        pendingLabel={toggle.pending}
        cancelLabel={toggle.back}
        variant={item.active ? 'danger' : 'secondary'}
        action={() => updateItemAction({ id: item.id, version: item.version, changes: { active: !item.active } })}
      />
    </div>
  );
}

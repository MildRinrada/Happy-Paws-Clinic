'use client';

import { ConfirmButton } from '@/features/integrations/components/confirm-button';
import { cancelPurchaseOrderAction } from '../actions/manage-inventory-actions';
import { purchaseOrderListText } from '../content/purchase-order-list';

/** Calls off what is outstanding on an order, after a second press that says received stock stays. */
export function CancelPurchaseOrderButton({ id, version }: { id: string; version: number }) {
  const text = purchaseOrderListText.cancel;

  return (
    <ConfirmButton
      label={text.button}
      confirmText={text.confirm}
      confirmLabel={text.confirmButton}
      pendingLabel={text.pending}
      cancelLabel={text.back}
      action={() => cancelPurchaseOrderAction({ id, version })}
    />
  );
}

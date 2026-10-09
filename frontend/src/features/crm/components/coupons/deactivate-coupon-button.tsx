'use client';

import { ConfirmButton } from '@/features/integrations/components/confirm-button';
import { deactivateCouponAction } from '../../actions/crm-manage-actions';
import { couponListText } from '../../content/coupon-list';

/** Switches a coupon off after a second press, saying that it cannot be switched back on. */
export function DeactivateCouponButton({ id }: { id: string }) {
  const text = couponListText.deactivate;

  return (
    <ConfirmButton
      label={text.button}
      confirmText={text.confirm}
      confirmLabel={text.confirmButton}
      pendingLabel={text.pending}
      cancelLabel={text.back}
      action={() => deactivateCouponAction({ id })}
    />
  );
}

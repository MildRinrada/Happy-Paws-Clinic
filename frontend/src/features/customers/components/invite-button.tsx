'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { inviteCustomerAction } from '../actions/customer-actions';
import { customerDetail } from '../content/customer-detail';

/** Sends, or re-sends, the portal invitation. The card above re-reads itself afterwards. */
export function InviteButton({ customerId, again }: { customerId: string; again: boolean }) {
  const text = customerDetail.portal;
  const [error, setError] = useState<string>();
  const [sent, setSent] = useState(false);
  const [pending, startTransition] = useTransition();

  function invite() {
    setError(undefined);
    setSent(false);

    startTransition(async () => {
      const result = await inviteCustomerAction({ customerId });

      if (result.ok) setSent(true);
      else setError(result.error.message);
    });
  }

  return (
    <div className="space-y-2">
      <Button variant="secondary" size="sm" disabled={pending} onClick={invite}>
        {pending ? text.sending : again ? text.resend : text.invite}
      </Button>
      {sent && <p role="status" className="text-sm text-emerald-700">{text.sent}</p>}
      {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
    </div>
  );
}

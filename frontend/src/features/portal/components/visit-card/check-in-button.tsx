'use client';

import { useState, useTransition } from 'react';
import { checkInAction } from '../../actions/check-in';
import { visitCard } from '../../content/visit-card';
import { solidButton } from './visit-button-styles';

interface CheckInButtonProps {
  appointmentId: string;
  /** Where a refusal is shown: the card puts it under the whole row of buttons. */
  onError: (message: string | undefined) => void;
  /** Placement in the card's row of buttons, such as taking the whole row. */
  className?: string;
}

/**
 * Checks in from the phone.
 *
 * The idempotency key is kept across attempts, so a retry after a dropped connection cannot put
 * the pet in the queue twice. It is replaced only after the backend has given a definite refusal
 * ("ยังไม่ถึงเวลาเช็กอิน"), so that trying again later is a new request and not a replay of the
 * stored "no".
 */
export function CheckInButton({ appointmentId, onError, className = '' }: CheckInButtonProps) {
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  const [pending, startTransition] = useTransition();

  function checkIn() {
    onError(undefined);
    startTransition(async () => {
      try {
        const result = await checkInAction({ appointmentId, idempotencyKey });

        if (!result.ok) {
          onError(result.error.message);
          setIdempotencyKey(crypto.randomUUID());
        }
      } catch {
        onError(visitCard.unreachable);
      }
    });
  }

  return (
    <button type="button" onClick={checkIn} disabled={pending} className={`${solidButton} ${className}`}>
      {pending ? visitCard.checkingIn : visitCard.checkIn}
    </button>
  );
}

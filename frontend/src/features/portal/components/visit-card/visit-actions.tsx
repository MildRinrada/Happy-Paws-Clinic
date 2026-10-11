'use client';

import Link from 'next/link';
import { useState } from 'react';
import { visitCard } from '../../content/visit-card';
import { CheckInButton } from './check-in-button';
import { outlineButton, solidButton } from './visit-button-styles';

interface VisitActionsProps {
  appointmentId?: string;
  canCheckIn: boolean;
  canReschedule: boolean;
}

/**
 * The buttons at the bottom of the card, two to a row. The one to press now is solid white:
 * check-in while it is open, otherwise the chat. Beside the chat goes "เลื่อนนัด" while the
 * appointment can still be moved online, and "ดูนัดหมาย" after that. With an odd number of
 * buttons the first takes the whole row.
 */
export function VisitActions({ appointmentId, canCheckIn, canReschedule }: VisitActionsProps) {
  const [error, setError] = useState<string>();

  const checkIn = canCheckIn && appointmentId !== undefined;
  const second = appointmentId
    ? canReschedule
      ? { label: visitCard.reschedule.label, href: visitCard.reschedule.href(appointmentId) }
      : visitCard.appointments
    : undefined;
  const count = Number(checkIn) + 1 + Number(second !== undefined);
  const wide = count % 2 === 1 ? 'col-span-2' : '';

  return (
    <div>
      <div className="grid grid-cols-2 gap-3">
        {checkIn && appointmentId && <CheckInButton appointmentId={appointmentId} onError={setError} className={wide} />}

        <Link href={visitCard.chat.href} className={checkIn ? outlineButton : `${solidButton} ${wide}`}>
          {visitCard.chat.label}
        </Link>

        {second && (
          <Link href={second.href} className={outlineButton}>
            {second.label}
          </Link>
        )}
      </div>

      {error && (
        <p role="alert" className="mt-3 rounded-lg bg-white/95 px-3 py-2 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}

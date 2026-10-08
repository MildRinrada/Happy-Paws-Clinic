'use client';

import Link from 'next/link';
import { useState } from 'react';
import { visitCard } from '../../content/visit-card';
import { CheckInButton } from './check-in-button';
import { outlineButton } from './visit-button-styles';

interface VisitActionsProps {
  appointmentId?: string;
  canCheckIn: boolean;
  canReschedule: boolean;
}

/** The row of buttons at the bottom of the card. Each shows only when it can be used. */
export function VisitActions({ appointmentId, canCheckIn, canReschedule }: VisitActionsProps) {
  const [error, setError] = useState<string>();

  return (
    <div>
      <div className="flex gap-3">
        {canCheckIn && appointmentId && <CheckInButton appointmentId={appointmentId} onError={setError} />}

        {canReschedule && appointmentId && (
          <Link href={visitCard.reschedule.href(appointmentId)} className={outlineButton}>
            {visitCard.reschedule.label}
          </Link>
        )}

        <Link href={visitCard.chat.href} className={outlineButton}>
          {visitCard.chat.label}
        </Link>
      </div>

      {error && (
        <p role="alert" className="mt-3 rounded-lg bg-white/95 px-3 py-2 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}

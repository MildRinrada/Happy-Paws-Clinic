'use client';

import Link from 'next/link';
import { useState } from 'react';
import { portalAppointments } from '../../content/portal-appointments';
import { portalPaths } from '../../content/portal-navigation';
import { dangerAction, plainAction } from './action-styles';
import { CancelPanel } from './cancel-panel';

/**
 * "เลื่อนนัด" and "ยกเลิกนัด" under an upcoming appointment. Cancelling opens a short panel that
 * says what a late cancellation means before anything is sent.
 */
export function AppointmentActions({ appointmentId }: { appointmentId: string }) {
  const [cancelling, setCancelling] = useState(false);
  const text = portalAppointments;

  if (cancelling) {
    return <CancelPanel appointmentId={appointmentId} onClose={() => setCancelling(false)} />;
  }

  return (
    <div className="mt-2 flex flex-wrap gap-2">
      <Link href={portalPaths.reschedule(appointmentId)} className={plainAction}>
        {text.reschedule}
      </Link>
      <button type="button" onClick={() => setCancelling(true)} className={dangerAction}>
        {text.cancel.open}
      </button>
    </div>
  );
}

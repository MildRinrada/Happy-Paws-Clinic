'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { openConsultationAction } from '../actions/consultation-actions';
import { consultationPage } from '../content/consultation-page';

interface OpenCaseButtonProps {
  ticketId?: string;
  appointmentId?: string;
  /** The patient is already in the room: the button goes back to the case rather than starting one. */
  resume?: boolean;
  size?: 'sm' | 'md';
}

/**
 * Takes the patient in and opens the case page. Pressing it for a visit whose case is already open
 * lands on that same case, so it doubles as the way back in.
 */
export function OpenCaseButton({ ticketId, appointmentId, resume = false, size = 'sm' }: OpenCaseButtonProps) {
  const text = consultationPage.open;
  const [key] = useState(() => crypto.randomUUID());
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function open() {
    setError(undefined);
    startTransition(async () => {
      // Success navigates to the case; only a refusal comes back.
      const result = await openConsultationAction({ ticketId, appointmentId, idempotencyKey: key });
      if (!result.ok) setError(result.error.message);
    });
  }

  return (
    <span className="inline-flex flex-col gap-1">
      <Button size={size} disabled={pending} onClick={open}>
        {pending ? text.working : resume ? text.resume : text.start}
      </Button>
      {error && <span role="alert" className="text-sm text-red-700">{error}</span>}
    </span>
  );
}

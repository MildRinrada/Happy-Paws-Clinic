'use client';

import Link from 'next/link';
import { useId, useState, useTransition } from 'react';
import { Alert } from '@/components/ui/alert';
import { cancelAction } from '../../actions/appointment-actions';
import { portalAppointments } from '../../content/portal-appointments';
import { portalPaths } from '../../content/portal-navigation';
import { TextAreaField } from '../shared/text-area-field';
import { dangerSolid, plainAction } from './action-styles';

interface CancelPanelProps {
  appointmentId: string;
  onClose: () => void;
}

/**
 * Confirming a cancellation. The policy line comes first: cancelling is never refused, but one
 * made too close to the visit is counted against the owner, and the clinic does not let the
 * portal read its exact cut-off or fee, so the line says what happens without numbers.
 *
 * On success the page refreshes and the row itself shows "ยกเลิก", with a note when the backend
 * counted it as late.
 */
export function CancelPanel({ appointmentId, onClose }: CancelPanelProps) {
  const text = portalAppointments.cancel;
  const reasonId = useId();
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function confirm() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await cancelAction({ appointmentId, reason: reason.trim() || undefined });
        if (!result.ok) setError(result.error.message);
      } catch {
        setError(text.unreachable);
      }
    });
  }

  return (
    <div className="mt-3 space-y-3 rounded-xl border border-red-100 bg-red-50/50 p-4">
      <p className="text-sm font-semibold text-ink-900">{text.title}</p>
      <p className="text-sm text-ink-700">{text.policy}</p>
      <p className="text-sm text-ink-700">
        {text.rescheduleHint}{' '}
        <Link href={portalPaths.reschedule(appointmentId)} className="font-semibold text-site-teal hover:underline">
          {text.rescheduleInstead}
        </Link>
      </p>

      <TextAreaField
        id={reasonId}
        label={text.reason}
        placeholder={text.reasonPlaceholder}
        rows={2}
        maxLength={500}
        value={reason}
        onChange={(event) => setReason(event.target.value)}
      />

      {error && <Alert>{error}</Alert>}

      <div className="flex flex-wrap gap-2">
        <button type="button" onClick={confirm} disabled={pending} className={dangerSolid}>
          {pending ? text.submitting : text.submit}
        </button>
        <button type="button" onClick={onClose} disabled={pending} className={plainAction}>
          {text.keep}
        </button>
      </div>
    </div>
  );
}

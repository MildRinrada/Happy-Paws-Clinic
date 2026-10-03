'use client';

import { Alert } from '@/components/ui/alert';
import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { bookingPage } from '../content/booking-page';
import type { SlotHold } from '../types/booking.types';
import { HoldCountdown } from './hold-countdown';
import { SummaryRow } from './summary-row';

export interface BookingSummary {
  pet?: string;
  service?: string;
  type: string;
  vet?: string;
  branch?: string;
  reason?: string;
}

interface ConfirmStepProps {
  hold: SlotHold;
  summary: BookingSummary;
  expired: boolean;
  onExpire: () => void;
  submitting: boolean;
  error?: string;
  onConfirm: () => void;
  onBack: () => void;
}

/** Step 3: everything chosen, how long the time is kept, and the button that books it. */
export function ConfirmStep({ hold, summary, expired, onExpire, submitting, error, onConfirm, onBack }: ConfirmStepProps) {
  const text = bookingPage.confirm;

  return (
    <div className="space-y-5">
      <h2 className="text-base font-semibold text-ink-900">{text.title}</h2>

      <dl className="divide-y divide-ink-100 rounded-xl bg-site-soft px-4">
        <SummaryRow label={text.pet} value={summary.pet} />
        <SummaryRow label={text.service} value={summary.service} />
        <SummaryRow label={text.type} value={summary.type} />
        <SummaryRow label={text.when} value={`${formatLongDate(hold.starts_at)} ${formatTime(hold.starts_at)}`} />
        <SummaryRow label={text.vet} value={summary.vet} />
        <SummaryRow label={text.branch} value={summary.branch} />
        <SummaryRow label={text.reason} value={summary.reason} />
      </dl>

      {expired ? (
        <Alert>{text.expired}</Alert>
      ) : (
        <p className="text-sm text-ink-700">
          {text.held(formatTime(hold.expires_at))} (<HoldCountdown expiresAt={hold.expires_at} onExpire={onExpire} />)
        </p>
      )}

      {error && <Alert>{error}</Alert>}

      <div className="flex flex-col gap-3 sm:flex-row">
        <button
          type="button"
          onClick={onConfirm}
          disabled={expired || submitting}
          className="h-12 flex-1 rounded-xl bg-site-orange text-sm font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {submitting ? text.submitting : text.submit}
        </button>
        <button
          type="button"
          onClick={onBack}
          disabled={submitting}
          className="h-12 rounded-xl border border-ink-200 px-5 text-sm font-semibold text-ink-700 hover:bg-ink-50"
        >
          {text.back}
        </button>
      </div>
    </div>
  );
}

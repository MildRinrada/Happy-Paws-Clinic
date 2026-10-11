'use client';

import { Alert } from '@/components/ui/alert';
import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { bookingPage } from '../content/booking-page';
import type { SlotHold } from '../types/booking.types';
import { StepActions } from './frame/step-actions';
import { StepBackButton } from './frame/step-back-button';
import { StepCard } from './frame/step-card';
import { StepNextButton } from './frame/step-next-button';
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
    <StepCard title={text.title}>
      <div className="space-y-5">
        {/* On a wide screen the summary card beside the step says all of this. */}
        <dl className="divide-y divide-ink-100 rounded-xl bg-site-soft px-4 lg:hidden">
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
          <p className="rounded-xl bg-site-soft px-4 py-3 text-sm text-ink-800">
            {text.held(formatTime(hold.expires_at))} (<HoldCountdown expiresAt={hold.expires_at} onExpire={onExpire} />)
          </p>
        )}

        {error && <Alert>{error}</Alert>}
      </div>

      <StepActions
        back={
          <StepBackButton onClick={onBack} disabled={submitting}>
            {text.back}
          </StepBackButton>
        }
        next={
          <StepNextButton onClick={onConfirm} disabled={expired || submitting}>
            {submitting ? text.submitting : text.submit}
          </StepNextButton>
        }
      />
    </StepCard>
  );
}

'use client';

import { Alert } from '@/components/ui/alert';
import { SummaryRow } from '@/features/booking/components/summary-row';
import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { portalReschedule } from '../../content/portal-reschedule';
import { RescheduleRefused } from './reschedule-refused';

export interface RescheduleRefusal {
  message: string;
  closedAt?: string;
}

interface RescheduleConfirmProps {
  fromAt: string;
  toAt: string;
  vet: string;
  submitting: boolean;
  error?: string;
  /** Set once the backend has said the cut-off has passed. Nothing more can be done here. */
  refused?: RescheduleRefusal;
  onConfirm: () => void;
  onBack: () => void;
}

const when = (iso: string) => `${formatLongDate(iso)} ${formatTime(iso)}`;

/**
 * The old time, the new one, and what moving means: the appointment waits for confirmation again,
 * and the new time is not held (the reschedule endpoint takes no hold), so it can still be taken.
 */
export function RescheduleConfirm({ fromAt, toAt, vet, submitting, error, refused, onConfirm, onBack }: RescheduleConfirmProps) {
  const text = portalReschedule.confirm;

  return (
    <div className="space-y-5">
      <h2 className="text-base font-semibold text-ink-900">{text.title}</h2>

      <dl className="divide-y divide-ink-100 rounded-xl bg-site-soft px-4">
        <SummaryRow label={text.from} value={when(fromAt)} />
        <SummaryRow label={text.to} value={when(toAt)} />
        <SummaryRow label={text.vet} value={vet} />
      </dl>

      {refused ? (
        <RescheduleRefused message={refused.message} closedAt={refused.closedAt} />
      ) : (
        <>
          <Alert tone="info">
            <p>{text.reconfirm}</p>
            <p className="mt-1">{text.notHeld}</p>
          </Alert>

          {error && <Alert>{error}</Alert>}

          <div className="flex flex-col gap-3 sm:flex-row">
            <button
              type="button"
              onClick={onConfirm}
              disabled={submitting}
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
        </>
      )}
    </div>
  );
}

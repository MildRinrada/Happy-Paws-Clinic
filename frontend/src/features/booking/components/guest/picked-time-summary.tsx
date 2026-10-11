import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { guestBookingPage } from '../../content/guest-booking-page';
import { SummaryRow } from '../summary-row';

export interface PickedSummary {
  service?: string;
  startsAt: string;
  vet?: string;
  branch?: string;
}

/**
 * What the visitor chose, kept in view inside the step on a phone, where the summary card sits
 * under the step. On a wide screen the summary card beside the step already says it.
 */
export function PickedTimeSummary({ summary }: { summary: PickedSummary }) {
  const text = guestBookingPage.summary;

  return (
    <dl className="divide-y divide-ink-100 rounded-xl bg-site-soft px-4 lg:hidden">
      <SummaryRow label={text.service} value={summary.service} />
      <SummaryRow label={text.when} value={`${formatLongDate(summary.startsAt)} ${formatTime(summary.startsAt)}`} />
      <SummaryRow label={text.vet} value={summary.vet} />
      <SummaryRow label={text.branch} value={summary.branch} />
    </dl>
  );
}

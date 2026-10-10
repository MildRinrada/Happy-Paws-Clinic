import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { guestBookingPage } from '../../content/guest-booking-page';
import { SummaryRow } from '../summary-row';

export interface PickedSummary {
  service?: string;
  startsAt: string;
  vet?: string;
  branch?: string;
}

/** What the visitor chose, kept in view while they confirm with their phone. */
export function PickedTimeSummary({ summary }: { summary: PickedSummary }) {
  const text = guestBookingPage.summary;

  return (
    <dl className="divide-y divide-ink-100 rounded-xl bg-site-soft px-4">
      <SummaryRow label={text.service} value={summary.service} />
      <SummaryRow label={text.when} value={`${formatLongDate(summary.startsAt)} ${formatTime(summary.startsAt)}`} />
      <SummaryRow label={text.vet} value={summary.vet} />
      <SummaryRow label={text.branch} value={summary.branch} />
    </dl>
  );
}

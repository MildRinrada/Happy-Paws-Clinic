import Link from 'next/link';
import { channelLabels, errorLabels, labelOf } from '../content/delivery-labels';
import { sendResultText } from '../content/send-page';
import type { SendResult } from '../types/notification.types';
import { DeliveryStatusBadge } from './delivery-status-badge';

interface SendResultListProps {
  result: SendResult;
  /** Customer names by id, for the people the screen already knows. */
  names: Record<string, string>;
}

/** What happened for each customer of a send or a broadcast, with a link to the same rows in the log. */
export function SendResultList({ result, names }: SendResultListProps) {
  return (
    <section aria-labelledby="send-result" className="space-y-3 rounded-xl border border-ink-200 bg-white px-5 py-4">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 id="send-result" className="font-display text-base font-semibold text-ink-900">
          {sendResultText.heading}
        </h2>
        <Link href={`/notifications/log?batch_id=${encodeURIComponent(result.batch_id)}`} className="text-sm text-brand-700 hover:underline">
          {sendResultText.viewLog}
        </Link>
      </div>

      <p className="text-sm text-ink-700">{sendResultText.summary(result.sent, result.failed, result.skipped)}</p>
      {result.quota && <p className="text-sm text-ink-600">{sendResultText.quota(result.quota.remaining, result.quota.limit)}</p>}

      <ul className="divide-y divide-ink-100 text-sm">
        {result.results.map((item) => (
          <li key={item.customer_id} className="flex flex-wrap items-center justify-between gap-2 py-2">
            <span className="font-medium text-ink-900">{names[item.customer_id] ?? item.customer_id}</span>
            <span className="flex flex-wrap items-center gap-2 text-ink-600">
              {item.channel && <span>{labelOf(channelLabels, item.channel)}</span>}
              {item.fallback_from && <span>{sendResultText.viaFallback(labelOf(channelLabels, item.fallback_from))}</span>}
              {item.error_code && <span>{labelOf(errorLabels, item.error_code)}</span>}
              <DeliveryStatusBadge status={item.status} />
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}

import { DeliveryStatusBadge } from '@/features/notifications/components/delivery-status-badge';
import { channelLabels, errorLabels, labelOf } from '@/features/notifications/content/delivery-labels';
import { messagesPage } from '../content/messages-page';
import type { ThreadDelivery } from '../types/thread.types';

/** What became of a reply: the result, the channel that carried it, and why if it did not go. */
export function MessageDelivery({ delivery }: { delivery: ThreadDelivery }) {
  return (
    <span className="mt-1 flex flex-wrap items-center justify-end gap-x-2 gap-y-1 text-xs text-ink-600">
      <DeliveryStatusBadge status={delivery.status} />
      {delivery.channel && <span>{labelOf(channelLabels, delivery.channel)}</span>}
      {delivery.fallback_from && <span>{messagesPage.thread.fallbackFrom(labelOf(channelLabels, delivery.fallback_from))}</span>}
      {delivery.error_code && <span>{labelOf(errorLabels, delivery.error_code)}</span>}
    </span>
  );
}

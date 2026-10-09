import { formatDateTime } from '@/lib/format/datetime';
import { deliveryLogPage, webhookEventLabels } from '../content/webhook-settings';
import type { WebhookDelivery } from '../types/integration.types';
import { WebhookDeliveryResult } from './webhook-delivery-result';
import { WebhookDeliveryStatusBadge } from './delivery-status-badge';

/** One row per event sent to the webhook, with how its latest attempt went. */
export function WebhookDeliveryTable({ rows }: { rows: WebhookDelivery[] }) {
  const columns = deliveryLogPage.columns;

  return (
    <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
      <table className="w-full min-w-[720px] text-sm">
        <thead className="border-b border-ink-100 text-left text-ink-500">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">{columns.time}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.event}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.status}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.attempts}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.result}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {rows.map((row) => (
            <tr key={row.id} className="align-top">
              <td className="whitespace-nowrap px-4 py-3 tabular-nums text-ink-700">{formatDateTime(row.created_at)}</td>
              <td className="px-4 py-3 text-ink-800">
                {webhookEventLabels[row.event_type] ?? row.event_type}
                <span className="block font-mono text-xs text-ink-500">{row.event_id}</span>
              </td>
              <td className="px-4 py-3">
                <WebhookDeliveryStatusBadge status={row.status} />
              </td>
              <td className="whitespace-nowrap px-4 py-3 tabular-nums text-ink-700">
                {deliveryLogPage.attempts(row.attempts, row.max_attempts)}
                {row.status === 'pending' && row.next_attempt_at && row.attempts > 0 && (
                  <span className="block text-xs text-ink-500">{deliveryLogPage.nextAttempt(formatDateTime(row.next_attempt_at))}</span>
                )}
              </td>
              <td className="px-4 py-3">
                <WebhookDeliveryResult delivery={row} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

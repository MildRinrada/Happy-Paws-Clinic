import Link from 'next/link';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { formatDateTime } from '@/lib/format/datetime';
import { channelLabels, errorLabels, labelOf, sourceLabels, templateLabels } from '../content/delivery-labels';
import { deliveryLogPage } from '../content/delivery-log';
import type { Delivery } from '../types/notification.types';
import { DeliveryStatusBadge } from './delivery-status-badge';

/** One row per attempt. A fallback shows as its own row, naming the channel it stood in for. */
export function DeliveryLogTable({ rows }: { rows: Delivery[] }) {
  const columns = deliveryLogPage.columns;

  return (
    <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
      <table className="w-full min-w-[720px] text-sm">
        <thead className="border-b border-ink-100 text-left text-ink-500">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">{columns.time}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.customer}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.template}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.channel}</th>
            <th scope="col" className="px-4 py-3 font-medium">{columns.status}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {rows.map((row) => (
            <tr key={row.id} className="align-top hover:bg-ink-50">
              <td className="whitespace-nowrap px-4 py-3 tabular-nums text-ink-700">{formatDateTime(row.attempted_at)}</td>
              <td className="px-4 py-3">
                <Link href={customerPaths.detail(row.customer_id)} className="font-medium text-brand-700 hover:underline">
                  {row.customer_name ?? row.customer_id}
                </Link>
              </td>
              <td className="px-4 py-3 text-ink-800">
                {labelOf(templateLabels, row.template)}
                <span className="block text-xs text-ink-500">{labelOf(sourceLabels, row.source)}</span>
              </td>
              <td className="px-4 py-3 text-ink-800">
                {labelOf(channelLabels, row.channel)}
                {row.recipient && <span className="block font-mono text-xs text-ink-500">{row.recipient}</span>}
                {row.fallback_from && (
                  <span className="block text-xs text-ink-500">
                    {deliveryLogPage.fallbackFrom(labelOf(channelLabels, row.fallback_from))}
                  </span>
                )}
              </td>
              <td className="px-4 py-3">
                <DeliveryStatusBadge status={row.status} />
                {row.error_code && <span className="mt-1 block text-xs text-ink-600">{labelOf(errorLabels, row.error_code)}</span>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

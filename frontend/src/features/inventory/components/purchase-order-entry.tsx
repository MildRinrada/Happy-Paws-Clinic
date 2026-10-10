import { formatDateTime } from '@/lib/format/datetime';
import { formatBaht } from '@/lib/format/money';
import { purchaseOrderListText } from '../content/purchase-order-list';
import type { PurchaseOrder } from '../types/inventory.types';
import { CancelPurchaseOrderButton } from './cancel-purchase-order-button';
import { PurchaseOrderStatusBadge } from './purchase-order-status-badge';

/** One order: number, supplier, status, each line's progress, cost, and the cancel button while anything is outstanding. */
export function PurchaseOrderEntry({ order }: { order: PurchaseOrder }) {
  const text = purchaseOrderListText;
  const cancellable = order.status === 'ordered' || order.status === 'partially_received';

  return (
    <li className="space-y-2 py-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <span className="font-mono font-medium text-ink-900">{order.number}</span>
          <span className="ml-2 text-ink-700">{order.supplier_name}</span>
          <span className="ml-3 text-xs text-ink-500">{text.orderedAt(formatDateTime(order.ordered_at))}</span>
        </div>
        <PurchaseOrderStatusBadge status={order.status} />
      </div>
      <ul className="space-y-0.5 text-sm text-ink-700">
        {order.lines.map((line) => (
          <li key={line.id} className="tabular-nums">
            {text.line(line.item_name, line.received_quantity, line.quantity, line.unit)}
            {line.cancelled_quantity > 0 && <span className="ml-2 text-xs text-ink-500">{text.cancelledLine(line.cancelled_quantity)}</span>}
          </li>
        ))}
      </ul>
      <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-ink-600">
        <span className="tabular-nums">
          {text.total(formatBaht(order.total_cost_satang))}, {text.receivedCost(formatBaht(order.received_cost_satang))}
        </span>
        {order.status === 'cancelled' && <span>{text.cancelledNote(order.cancel_reason)}</span>}
        {cancellable && <CancelPurchaseOrderButton id={order.id} version={order.version} />}
      </div>
    </li>
  );
}

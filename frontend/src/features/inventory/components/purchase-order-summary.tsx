import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { formatBaht } from '@/lib/format/money';
import { orderStatusLabels } from '../content/inventory-labels';
import { purchaseOrderPage } from '../content/purchase-order-page';
import type { PurchaseOrder } from '../types/inventory.types';

/** The order just raised: its number to quote to the supplier, its lines and its cost. */
export function PurchaseOrderSummary({ order, onAnother }: { order: PurchaseOrder; onAnother: () => void }) {
  const text = purchaseOrderPage.result;

  return (
    <Alert tone="success">
      <p className="font-medium">
        {text.heading(order.number)} ({orderStatusLabels[order.status]})
      </p>
      <ul className="mt-1 space-y-0.5">
        {order.lines.map((line) => (
          <li key={line.id}>{text.line(line.item_name, line.quantity, line.unit, formatBaht(line.line_total_satang))}</li>
        ))}
      </ul>
      <p className="mt-1 font-medium">{purchaseOrderPage.total(formatBaht(order.total_cost_satang))}</p>
      <p className="mt-1">{text.next}</p>
      <Button size="sm" variant="secondary" className="mt-2" onClick={onAnother}>
        {text.another}
      </Button>
    </Alert>
  );
}

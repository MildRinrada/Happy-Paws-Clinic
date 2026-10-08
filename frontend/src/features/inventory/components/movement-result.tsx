import { Alert } from '@/components/ui/alert';
import { movementLabels, orderStatusLabels } from '../content/inventory-labels';
import { movementPage } from '../content/movement-page';
import { stockAt } from '../lib/stock-at';
import type { StockChange } from '../types/inventory.types';

/** What a movement wrote: one line per ledger row, the stock now, and the order when there was one. */
export function MovementResult({ change, branchId }: { change: StockChange; branchId?: string }) {
  const text = movementPage.result;
  const stock = stockAt(change.item, branchId);

  return (
    <Alert tone="success">
      <p className="font-medium">{text.heading}</p>
      <ul className="mt-1 space-y-0.5">
        {change.movements.map((row) => (
          <li key={row.id}>{text.row(movementLabels[row.type], row.quantity, row.lot_number, row.balance_after)}</li>
        ))}
      </ul>
      <p className="mt-1">{text.available(change.item.name, stock.available, change.item.unit)}</p>
      {change.purchase_order && (
        <p className="mt-1">{text.order(change.purchase_order.number, orderStatusLabels[change.purchase_order.status])}</p>
      )}
    </Alert>
  );
}

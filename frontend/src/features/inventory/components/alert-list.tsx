import type { InventoryAlert } from '../types/inventory.types';
import { AlertRow } from './alert-row';

/** The alerts, in the order the backend ranks them: worst first. */
export function AlertList({ alerts }: { alerts: InventoryAlert[] }) {
  return (
    <ul className="divide-y divide-ink-100 rounded-xl border border-ink-200 bg-white">
      {alerts.map((alert) => (
        <AlertRow key={`${alert.kind}-${alert.item_id}-${alert.branch_id}-${alert.lot_id ?? ''}`} alert={alert} />
      ))}
    </ul>
  );
}

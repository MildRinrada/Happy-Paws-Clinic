import { Badge } from '@/components/ui/badge';
import { alertLabels } from '../content/inventory-labels';
import { alertsPage } from '../content/alerts-page';
import { formatDay } from '../lib/format-day';
import type { InventoryAlert } from '../types/inventory.types';

/** One thing that needs attention: what, how bad, and what is already on its way. */
export function AlertRow({ alert }: { alert: InventoryAlert }) {
  const { label, tone } = alertLabels[alert.kind];
  const text = alertsPage;
  const lotAlert = alert.kind === 'expired' || alert.kind === 'near_expiry';

  return (
    <li className="flex flex-wrap items-start justify-between gap-3 px-4 py-3">
      <div className="min-w-0 space-y-0.5">
        <p className="font-medium text-ink-900">{alert.item_name}</p>
        {lotAlert && alert.lot_quantity !== undefined ? (
          <p className="text-sm text-ink-700">
            {text.lot(alert.lot_number, alert.lot_quantity)} {formatDay(alert.expires_on)}
            {alert.days_to_expiry !== undefined && <span className="text-ink-500"> ({text.expiresIn(alert.days_to_expiry)})</span>}
          </p>
        ) : (
          <p className="text-sm text-ink-700">
            {text.available(alert.available, alert.unit)} <span className="text-ink-500">({text.reorder(alert.reorder_level)})</span>
          </p>
        )}
        {alert.on_order > 0 && <p className="text-xs text-ink-500">{text.onOrder(alert.on_order)}</p>}
      </div>
      <Badge tone={tone}>{label}</Badge>
    </li>
  );
}

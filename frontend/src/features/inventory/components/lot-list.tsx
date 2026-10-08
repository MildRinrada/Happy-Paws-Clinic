import { itemsPage } from '../content/items-page';
import { formatDay } from '../lib/format-day';
import type { StockLot } from '../types/inventory.types';

/** The lots behind a number, soonest expiry first. Expired ones are marked: counted, not usable. */
export function LotList({ lots }: { lots: StockLot[] }) {
  if (lots.length === 0) {
    return <span className="text-ink-400">{itemsPage.none}</span>;
  }

  return (
    <ul className="space-y-0.5 text-xs text-ink-600">
      {lots.map((lot) => (
        <li key={lot.id} className={lot.expired ? 'text-red-700' : undefined}>
          {itemsPage.lot(lot.lot_number, lot.quantity_on_hand)}
          {lot.expires_on && <span className="text-ink-400"> ({formatDay(lot.expires_on)})</span>}
          {lot.expired && <span> {itemsPage.expiredLot}</span>}
        </li>
      ))}
    </ul>
  );
}

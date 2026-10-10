import { Badge } from '@/components/ui/badge';
import { formatBaht } from '@/lib/format/money';
import { itemEditText } from '../content/item-edit';
import { kindLabels } from '../content/inventory-labels';
import { itemsPage } from '../content/items-page';
import { formatDay } from '../lib/format-day';
import { stockAt } from '../lib/stock-at';
import type { InventoryItem } from '../types/inventory.types';
import { ItemManageCell } from './item-manage-cell';
import { LotList } from './lot-list';
import { StockStatusBadge } from './stock-status-badge';

/** One item at the branch in view. A retired item carries a badge and keeps its stock on show. */
export function ItemRow({ item, branchId, canEdit = false }: { item: InventoryItem; branchId?: string; canEdit?: boolean }) {
  const stock = stockAt(item, branchId);
  const text = itemsPage;

  return (
    <tr className="align-top hover:bg-ink-50">
      <td className="px-4 py-2.5">
        <span className="font-medium text-ink-900">{item.name}</span>
        {!item.active && (
          <span className="ml-2">
            <Badge tone="neutral">{itemEditText.retired}</Badge>
          </span>
        )}
        <span className="block text-xs text-ink-500">
          {text.meta(kindLabels[item.kind], item.reorder_level)}
        </span>
      </td>
      <td className="whitespace-nowrap px-4 py-2.5 tabular-nums text-ink-800">
        {formatBaht(item.unit_price_satang)}
        <span className="block text-xs text-ink-500">{text.perUnit(item.unit)}</span>
      </td>
      <td className="whitespace-nowrap px-4 py-2.5 tabular-nums">
        <span className="font-semibold text-ink-900">{stock.available}</span> <span className="text-ink-500">{item.unit}</span>
        {stock.on_hand !== stock.available && <span className="block text-xs text-ink-500">{text.onHand(stock.on_hand)}</span>}
      </td>
      <td className="px-4 py-2.5">
        <StockStatusBadge status={stock.status} />
      </td>
      <td className="whitespace-nowrap px-4 py-2.5 tabular-nums text-ink-700">{stock.on_order > 0 ? stock.on_order : text.none}</td>
      <td className="whitespace-nowrap px-4 py-2.5 text-ink-700">{formatDay(stock.next_expiry)}</td>
      <td className="px-4 py-2.5">
        <LotList lots={stock.lots} />
      </td>
      {canEdit && (
        <td className="px-4 py-2.5">
          <ItemManageCell item={item} />
        </td>
      )}
    </tr>
  );
}

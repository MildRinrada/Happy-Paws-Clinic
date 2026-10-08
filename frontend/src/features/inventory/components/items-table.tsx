import { itemsPage } from '../content/items-page';
import type { InventoryItem } from '../types/inventory.types';
import { ItemRow } from './item-row';

/** The catalogue with the stock of the branch in view, one row per item. */
export function ItemsTable({ items, branchId }: { items: InventoryItem[]; branchId?: string }) {
  const columns = itemsPage.columns;

  return (
    <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
      <table className="w-full min-w-[760px] text-sm">
        <thead className="border-b border-ink-100 text-left text-ink-500">
          <tr>
            <th scope="col" className="px-4 py-2.5 font-medium">{columns.name}</th>
            <th scope="col" className="px-4 py-2.5 font-medium">{columns.price}</th>
            <th scope="col" className="px-4 py-2.5 font-medium">{columns.available}</th>
            <th scope="col" className="px-4 py-2.5 font-medium">{columns.status}</th>
            <th scope="col" className="px-4 py-2.5 font-medium">{columns.onOrder}</th>
            <th scope="col" className="px-4 py-2.5 font-medium">{columns.expiry}</th>
            <th scope="col" className="px-4 py-2.5 font-medium">{columns.lots}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {items.map((item) => (
            <ItemRow key={item.id} item={item} branchId={branchId} />
          ))}
        </tbody>
      </table>
    </div>
  );
}

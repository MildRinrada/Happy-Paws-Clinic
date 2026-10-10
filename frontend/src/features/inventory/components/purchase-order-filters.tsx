import { buttonClass } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { purchaseOrderListText, purchaseOrderStatusLabels } from '../content/purchase-order-list';
import type { PurchaseOrderStatus } from '../types/inventory.types';

/** Supplier and status for the order list, a plain GET form on the page's own address. */
export function PurchaseOrderFilters({ supplier, status }: { supplier?: string; status?: PurchaseOrderStatus }) {
  const text = purchaseOrderListText.filters;

  return (
    <form method="get" role="search" aria-label={text.label} className="flex flex-wrap items-end gap-3">
      <label className="block w-52 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.supplier}</span>
        <Input name="supplier" type="search" defaultValue={supplier} maxLength={200} />
      </label>
      <label className="block w-40 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.status}</span>
        <Select name="status" defaultValue={status ?? ''}>
          <option value="">{text.any}</option>
          {(Object.keys(purchaseOrderStatusLabels) as PurchaseOrderStatus[]).map((value) => (
            <option key={value} value={value}>
              {purchaseOrderStatusLabels[value].text}
            </option>
          ))}
        </Select>
      </label>
      <button type="submit" className={buttonClass('secondary', 'md')}>
        {text.apply}
      </button>
    </form>
  );
}

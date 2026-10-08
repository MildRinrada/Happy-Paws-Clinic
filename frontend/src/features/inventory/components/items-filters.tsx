import Link from 'next/link';
import { Button, buttonClass } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { stockStatusLabels } from '../content/inventory-labels';
import { itemsPage } from '../content/items-page';
import { inventoryPaths } from '../lib/inventory-paths';
import type { ItemsQuery, StockStatus } from '../types/inventory.types';

const STATUSES: StockStatus[] = ['low', 'out', 'ok'];

/** A plain GET form: the search lives in the address, so a filtered list can be bookmarked. */
export function ItemsFilters({ query }: { query: ItemsQuery }) {
  const text = itemsPage.filters;

  return (
    <form method="get" action={inventoryPaths.list} aria-label={text.label} className="flex flex-wrap items-end gap-3">
      <label className="block min-w-56 flex-1 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.search}</span>
        <Input name="q" defaultValue={query.q} placeholder={text.searchPlaceholder} maxLength={100} />
      </label>
      <label className="block w-40 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.status}</span>
        <Select name="status" defaultValue={query.status ?? ''}>
          <option value="">{text.any}</option>
          {STATUSES.map((status) => (
            <option key={status} value={status}>
              {stockStatusLabels[status].label}
            </option>
          ))}
        </Select>
      </label>
      <Button type="submit" variant="secondary">
        {text.apply}
      </Button>
      {(query.q || query.status) && (
        <Link href={inventoryPaths.list} className={buttonClass('ghost')}>
          {text.clear}
        </Link>
      )}
    </form>
  );
}

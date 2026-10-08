import type { Metadata } from 'next';
import { redirect } from 'next/navigation';
import { Alert } from '@/components/ui/alert';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listInventoryItems } from '@/features/inventory/api/listInventoryItems';
import { BranchNote } from '@/features/inventory/components/branch-note';
import { ItemsFilters } from '@/features/inventory/components/items-filters';
import { ItemsTable } from '@/features/inventory/components/items-table';
import { inventoryArea, inventoryTabs } from '@/features/inventory/content/inventory-tabs';
import { itemsPage } from '@/features/inventory/content/items-page';
import { readItemsQuery } from '@/features/inventory/lib/read-items-query';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: itemsPage.metaTitle };

interface InventoryPageProps {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/**
 * "สต๊อกยา": the catalogue with the stock of the branch in view, searchable by name and filterable
 * by status. A session that cannot read stock but can record or order it is sent to that tab.
 */
export default async function InventoryPage({ searchParams }: InventoryPageProps) {
  const context = await loadStaffContext();

  if (!context?.can(Scope.InventoryRead)) {
    const first = inventoryTabs.find((tab) => context?.can(tab.scope));

    if (first) {
      redirect(first.href);
    }

    return <Alert tone="info">{inventoryArea.noAccess}</Alert>;
  }

  if (!context.branch) {
    return <Alert tone="info">{inventoryArea.noBranch}</Alert>;
  }

  const branch = context.branch;
  const query = readItemsQuery(await searchParams);
  const items = await authed(() => listInventoryItems({ ...query, branch: branch.id }));
  const filtered = Boolean(query.q || query.status);

  return (
    <div className="space-y-5">
      <PageHeader title={itemsPage.title} subtitle={itemsPage.subtitle} actions={<BranchNote name={branch.name} />} />
      <ItemsFilters query={query} />
      {items.length === 0 ? (
        <EmptyState title={filtered ? itemsPage.noMatch : itemsPage.empty} />
      ) : (
        <>
          <p className="text-sm text-ink-500">{itemsPage.count(items.length)}</p>
          <ItemsTable items={items} branchId={branch.id} />
        </>
      )}
    </div>
  );
}

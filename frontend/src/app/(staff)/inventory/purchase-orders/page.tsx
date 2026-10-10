import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { withCursor } from '@/features/crm/lib/read-list-query';
import { listInventoryItems } from '@/features/inventory/api/listInventoryItems';
import { listPurchaseOrders } from '@/features/inventory/api/manageInventory';
import { BranchNote } from '@/features/inventory/components/branch-note';
import { PurchaseOrderFilters } from '@/features/inventory/components/purchase-order-filters';
import { PurchaseOrderForm } from '@/features/inventory/components/purchase-order-form';
import { PurchaseOrderList } from '@/features/inventory/components/purchase-order-list';
import { inventoryArea } from '@/features/inventory/content/inventory-tabs';
import { itemFields } from '@/features/inventory/content/item-fields';
import { purchaseOrderPage } from '@/features/inventory/content/purchase-order-page';
import { inventoryPaths } from '@/features/inventory/lib/inventory-paths';
import { readOrderQuery } from '@/features/inventory/lib/read-order-query';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: purchaseOrderPage.metaTitle };

/**
 * "สั่งซื้อจากซัพพลายเออร์" for the branch in view, and the branch's orders with the button to cancel
 * what is outstanding (docs/contract-extensions.md § 25). Retired items are not offered to order.
 */
export default async function InventoryPurchaseOrdersPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.InventoryPurchase)) {
    return <Alert tone="info">{purchaseOrderPage.noAccess}</Alert>;
  }

  if (!context.branch) {
    return <Alert tone="info">{inventoryArea.noBranch}</Alert>;
  }

  const branch = context.branch;
  const canRead = context.can(Scope.InventoryRead);
  const query = readOrderQuery(params);
  const [items, page] = await Promise.all([
    canRead ? authed(() => listInventoryItems({ branch: branch.id })) : Promise.resolve([]),
    authed(() => listPurchaseOrders({ ...query, branch: branch.id })),
  ]);
  const filters = { supplier: query.supplier, status: query.status };

  return (
    <div className="space-y-5">
      <PageHeader title={purchaseOrderPage.title} subtitle={purchaseOrderPage.subtitle} actions={<BranchNote name={branch.name} />} />
      {!canRead && <p className="text-sm text-ink-600">{itemFields.noRead}</p>}
      <Card>
        <CardBody>
          <PurchaseOrderForm branchId={branch.id} items={items.filter((item) => item.active)} />
        </CardBody>
      </Card>
      <PurchaseOrderList
        orders={page.data}
        filters={<PurchaseOrderFilters supplier={query.supplier} status={query.status} />}
        nextHref={page.page.next_cursor ? withCursor(inventoryPaths.purchaseOrders, filters, page.page.next_cursor) : undefined}
        firstHref={query.cursor ? withCursor(inventoryPaths.purchaseOrders, filters) : undefined}
      />
    </div>
  );
}

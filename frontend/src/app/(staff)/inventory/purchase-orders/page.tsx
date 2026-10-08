import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listInventoryItems } from '@/features/inventory/api/listInventoryItems';
import { BranchNote } from '@/features/inventory/components/branch-note';
import { PurchaseOrderForm } from '@/features/inventory/components/purchase-order-form';
import { inventoryArea } from '@/features/inventory/content/inventory-tabs';
import { itemFields } from '@/features/inventory/content/item-fields';
import { purchaseOrderPage } from '@/features/inventory/content/purchase-order-page';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: purchaseOrderPage.metaTitle };

/** "สั่งซื้อจากซัพพลายเออร์" for the branch in view. */
export default async function InventoryPurchaseOrdersPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.InventoryPurchase)) {
    return <Alert tone="info">{purchaseOrderPage.noAccess}</Alert>;
  }

  if (!context.branch) {
    return <Alert tone="info">{inventoryArea.noBranch}</Alert>;
  }

  const branch = context.branch;
  const canRead = context.can(Scope.InventoryRead);
  const items = canRead ? await authed(() => listInventoryItems({ branch: branch.id })) : [];

  return (
    <div className="space-y-5">
      <PageHeader title={purchaseOrderPage.title} subtitle={purchaseOrderPage.subtitle} actions={<BranchNote name={branch.name} />} />
      {!canRead && <p className="text-sm text-ink-600">{itemFields.noRead}</p>}
      <Card>
        <CardBody>
          <PurchaseOrderForm branchId={branch.id} items={items} />
        </CardBody>
      </Card>
    </div>
  );
}

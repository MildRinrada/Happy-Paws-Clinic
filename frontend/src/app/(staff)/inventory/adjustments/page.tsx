import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listInventoryItems } from '@/features/inventory/api/listInventoryItems';
import { AdjustmentForm } from '@/features/inventory/components/adjustment-form';
import { BranchNote } from '@/features/inventory/components/branch-note';
import { adjustmentPage } from '@/features/inventory/content/adjustment-page';
import { inventoryArea } from '@/features/inventory/content/inventory-tabs';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: adjustmentPage.metaTitle };

/** "ปรับยอดหลังนับสต๊อก" at the branch in view. Choosing what was counted needs the stock list too. */
export default async function InventoryAdjustmentsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.InventoryAdjust)) {
    return <Alert tone="info">{adjustmentPage.noAccess}</Alert>;
  }

  if (!context.can(Scope.InventoryRead)) {
    return <Alert tone="info">{adjustmentPage.noRead}</Alert>;
  }

  if (!context.branch) {
    return <Alert tone="info">{inventoryArea.noBranch}</Alert>;
  }

  const branch = context.branch;
  const items = await authed(() => listInventoryItems({ branch: branch.id }));

  return (
    <div className="space-y-5">
      <PageHeader title={adjustmentPage.title} subtitle={adjustmentPage.subtitle} actions={<BranchNote name={branch.name} />} />
      {items.length === 0 ? (
        <EmptyState title={adjustmentPage.empty} />
      ) : (
        <Card>
          <CardBody>
            <AdjustmentForm branchId={branch.id} items={items} />
          </CardBody>
        </Card>
      )}
    </div>
  );
}

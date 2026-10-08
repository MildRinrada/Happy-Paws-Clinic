import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listBranches } from '@/features/clinic/api/listBranches';
import { listInventoryItems } from '@/features/inventory/api/listInventoryItems';
import { BranchNote } from '@/features/inventory/components/branch-note';
import { MovementForm } from '@/features/inventory/components/movement-form';
import { inventoryArea } from '@/features/inventory/content/inventory-tabs';
import { movementPage } from '@/features/inventory/content/movement-page';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: movementPage.metaTitle };

/** "รับเข้า จ่ายออก โอนสาขา" at the branch in view. Stock may be sent to any open branch. */
export default async function InventoryMovementsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.InventoryWrite)) {
    return <Alert tone="info">{movementPage.noAccess}</Alert>;
  }

  if (!context.branch) {
    return <Alert tone="info">{inventoryArea.noBranch}</Alert>;
  }

  const branch = context.branch;
  const canRead = context.can(Scope.InventoryRead);
  const [items, branches] = await Promise.all([
    canRead ? authed(() => listInventoryItems({ branch: branch.id })) : Promise.resolve([]),
    listBranches(),
  ]);
  const destinations = branches.filter((other) => other.id !== branch.id).map((other) => ({ id: other.id, name: other.name }));

  return (
    <div className="space-y-5">
      <PageHeader title={movementPage.title} subtitle={movementPage.subtitle} actions={<BranchNote name={branch.name} />} />
      <Card>
        <CardBody>
          <MovementForm branchId={branch.id} items={items} canRead={canRead} destinations={destinations} />
        </CardBody>
      </Card>
    </div>
  );
}

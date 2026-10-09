import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { PackageSaleForm } from '@/features/crm/components/packages/package-sale-form';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { packageSalePage } from '@/features/crm/content/package-sale-page';
import { loadServiceOptions } from '@/features/crm/lib/load-service-options';
import { Scope } from '@/lib/auth/scopes';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: crmArea.metaTitle(packageSalePage.title) };

/** A package sold at the counter, at the branch in view. Its bill is paid at the cash desk. */
export default async function CrmPackagesPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.CrmManage)) {
    return <Alert tone="info">{crmArea.noManage}</Alert>;
  }

  if (!context.branch) {
    return <Alert tone="info">{crmArea.noBranch}</Alert>;
  }

  const branch = context.branch;
  const services = await loadServiceOptions(branch.id, context.can(Scope.ServiceManage));

  return (
    <div className="max-w-3xl space-y-5">
      <PageHeader
        title={packageSalePage.title}
        subtitle={packageSalePage.subtitle}
        actions={<span className="text-sm text-ink-600">{crmArea.branchNote(branch.name)}</span>}
      />
      <Card>
        <CardBody>
          <PackageSaleForm
            branchId={branch.id}
            canSearchCustomers={context.can(Scope.CustomerRead)}
            canReadLoyalty={context.can(Scope.CrmRead)}
            services={services}
            today={clinicDay(new Date())}
          />
        </CardBody>
      </Card>
    </div>
  );
}

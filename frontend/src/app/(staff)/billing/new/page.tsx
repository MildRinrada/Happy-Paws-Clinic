import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { CreateInvoiceForm } from '@/features/billing/components/create/create-invoice-form';
import { createInvoicePage } from '@/features/billing/content/create-invoice-page';
import { loadCatalogue } from '@/features/billing/lib/load-catalogue';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: createInvoicePage.metaTitle };

/**
 * A bill raised by hand: a counter sale, a boarding night, a charge with no case behind it. Bills
 * for treatment are raised by the vet closing the case and are not made here.
 */
export default async function NewInvoicePage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.InvoiceWrite)) {
    return <Alert tone="info">{createInvoicePage.noAccess}</Alert>;
  }

  if (!context.branch) {
    return <Alert tone="info">{createInvoicePage.noBranch}</Alert>;
  }

  const branch = context.branch;
  const catalogue = await loadCatalogue({
    branchId: branch.id,
    canManageServices: context.can(Scope.ServiceManage),
    canReadInventory: context.can(Scope.InventoryRead),
  });

  return (
    <div className="max-w-3xl space-y-5">
      <Link href="/billing" className="text-sm text-brand-700 hover:underline">
        {createInvoicePage.back}
      </Link>
      <PageHeader title={createInvoicePage.title} subtitle={createInvoicePage.subtitle} />
      <p className="text-sm text-ink-600">{branch.name}</p>
      <Card>
        <CardBody>
          <CreateInvoiceForm branchId={branch.id} canSearchCustomers={context.can(Scope.CustomerRead)} catalogue={catalogue} />
        </CardBody>
      </Card>
    </div>
  );
}

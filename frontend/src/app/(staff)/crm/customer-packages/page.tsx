import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listPackages } from '@/features/crm/api/listPackages';
import { CustomerLookup } from '@/features/crm/components/loyalty/customer-lookup';
import { PackageStatusFilter } from '@/features/crm/components/packages/package-status-filter';
import { PackageList } from '@/features/crm/components/shared/package-list';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { packageLookupText } from '@/features/crm/content/package-lookup';
import { readPackageQuery } from '@/features/crm/lib/read-list-query';
import { getCustomer } from '@/features/customers/api/getCustomer';
import { ApiError, optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: crmArea.metaTitle(packageLookupText.title) };

/** One customer's packages at every branch (docs/contract-extensions.md § 25), picked by search. */
export default async function CrmCustomerPackagesPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.CrmRead)) {
    return <Alert tone="info">{crmArea.noAccess}</Alert>;
  }

  const text = packageLookupText;
  const canReadCustomer = context.can(Scope.CustomerRead);
  const query = readPackageQuery(params);
  const found = query.customerId
    ? await Promise.all([
        authed(() => listPackages(query.customerId, query.status)),
        canReadCustomer ? optional(getCustomer(query.customerId)).catch(() => null) : Promise.resolve(null),
      ]).catch((error: unknown) => {
        if (error instanceof ApiError && error.status === 404) return null;
        throw error;
      })
    : undefined;

  return (
    <div className="space-y-6">
      <PageHeader title={text.title} subtitle={text.subtitle} />
      <CustomerLookup canSearch={canReadCustomer} target="customerPackages" />

      {found === null && <Alert>{text.notFound}</Alert>}
      {found && (
        <Card className="max-w-3xl">
          <CardHeader title={text.heading(found[1]?.customer.display_name ?? query.customerId)} />
          <CardBody className="space-y-4">
            <PackageStatusFilter customerId={query.customerId} status={query.status} />
            <PackageList packages={found[0]} emptyText={text.empty} />
          </CardBody>
        </Card>
      )}
    </div>
  );
}

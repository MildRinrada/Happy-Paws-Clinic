import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { Badge } from '@/components/ui/badge';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getCustomer } from '@/features/customers/api/getCustomer';
import { BalanceCard } from '@/features/customers/components/balance-card';
import { ContactCard } from '@/features/customers/components/contact-card';
import { CustomerPetsCard } from '@/features/customers/components/customer-pets-card';
import { LineAddressCard } from '@/features/customers/components/line/line-address-card';
import { MergeCustomerCard } from '@/features/customers/components/merge-customer-card';
import { PortalInviteCard } from '@/features/customers/components/portal-invite-card';
import { customerDetail } from '@/features/customers/content/customer-detail';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { REGISTERED_PARAM } from '@/features/customers/lib/registered-param';
import type { CustomerDetail } from '@/features/customers/types/customer.types';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: customerDetail.metaTitle };

/**
 * One customer, as the counter looks at them while they wait: who they are, what they owe,
 * whether they can follow their visit online, where their LINE messages go, and their animals,
 * with the form to add another.
 */
export default async function CustomerPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<Record<string, string | undefined>>;
}) {
  const [context, { id }, query] = await Promise.all([loadStaffContext(), params, searchParams]);
  const text = customerDetail;

  if (!context?.can(Scope.CustomerRead)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  let detail: CustomerDetail;
  try {
    detail = await authed(() => getCustomer(id));
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return <Alert>{text.notFound}</Alert>;
    }
    if (error instanceof ApiError && error.status === 403) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  const { customer } = detail;
  const justRegistered = query[REGISTERED_PARAM] === '1';

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <BackLink href={customerPaths.list}>{text.back}</BackLink>
        <PageHeader
          title={
            <span className="flex flex-wrap items-center gap-2">
              {customer.display_name}
              {!customer.is_active && <Badge tone="amber">{text.inactive}</Badge>}
            </span>
          }
          subtitle={customer.phone}
        />
      </div>

      {justRegistered && <Alert tone="success">{text.justRegistered}</Alert>}

      {customer.merged_into_customer_id && (
        <Alert tone="info">
          {text.merged}{' '}
          <Link href={customerPaths.detail(customer.merged_into_customer_id)} className="font-medium underline">
            {text.openMerged}
          </Link>
        </Alert>
      )}

      <div className="grid gap-6 lg:grid-cols-[1fr_20rem]">
        <CustomerPetsCard
          customerId={customer.id}
          pets={detail.pets}
          canAdd={context.can(Scope.PatientWrite) && !customer.merged_into_customer_id}
          addFirst={justRegistered}
        />

        <div className="space-y-6">
          <ContactCard customer={customer} />
          <BalanceCard outstandingSatang={detail.outstanding_satang} canOpenBilling={context.can(Scope.InvoiceWrite)} />
          <PortalInviteCard
            customer={customer}
            invitation={detail.invitation}
            canInvite={context.can(Scope.CustomerWrite)}
          />
          <LineAddressCard
            line={detail.line}
            customerName={customer.display_name}
            canEdit={context.can(Scope.CustomerWrite) && !customer.merged_into_customer_id}
          />
          {context.can(Scope.CustomerMerge) && !customer.merged_into_customer_id && (
            <MergeCustomerCard customer={customer} />
          )}
        </div>
      </div>
    </div>
  );
}

import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { CustomerLookup } from '@/features/crm/components/loyalty/customer-lookup';
import { LoyaltyOverview } from '@/features/crm/components/loyalty/loyalty-overview';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { loyaltyPage } from '@/features/crm/content/loyalty-page';
import { loadLoyaltyView } from '@/features/crm/lib/load-loyalty-view';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: crmArea.metaTitle(loyaltyPage.title) };

/** A customer's points and membership, picked by search; `?customer_id=` opens one directly. */
export default async function CrmLoyaltyPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, query] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.CrmRead)) {
    return <Alert tone="info">{crmArea.noAccess}</Alert>;
  }

  const canReadCustomer = context.can(Scope.CustomerRead);
  const customerId = typeof query.customer_id === 'string' ? query.customer_id.trim() : '';
  const view = customerId ? await loadLoyaltyView(customerId, canReadCustomer) : null;

  return (
    <div className="space-y-6">
      <PageHeader title={loyaltyPage.title} subtitle={loyaltyPage.subtitle} />
      <CustomerLookup canSearch={canReadCustomer} />

      {view?.kind === 'not_found' && <Alert>{loyaltyPage.notFound}</Alert>}
      {view?.kind === 'refused' && <Alert>{view.message}</Alert>}
      {view?.kind === 'found' && (
        <LoyaltyOverview
          loyalty={view.loyalty}
          customerName={view.customerName}
          customerPhone={view.customerPhone}
          canOpenCustomer={canReadCustomer}
        />
      )}
    </div>
  );
}

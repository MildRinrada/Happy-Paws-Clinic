import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { buttonClass } from '@/components/ui/button';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { CustomerLookup } from '@/features/crm/components/loyalty/customer-lookup';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { crmPaths } from '@/features/crm/lib/crm-paths';
import { readCursor, withCursor } from '@/features/crm/lib/read-list-query';
import { listStaffReferrals } from '@/features/referrals/api/staffReferrals';
import { ReferralFilterNote } from '@/features/referrals/components/staff/referral-filter-note';
import { ReferralList } from '@/features/referrals/components/staff/referral-list';
import { staffReferralsText } from '@/features/referrals/content/staff-referrals';
import { referralPaths } from '@/features/referrals/lib/referral-paths';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: crmArea.metaTitle(staffReferralsText.title) };

/**
 * Who referred whom (docs/contract-extensions.md § 39), on `crm:read`: newest first, with the status,
 * the reason a referral earned nothing, and the points each side got. `?customer=` narrows to the
 * referrals where one customer is either side. The points and the cap are set on the loyalty
 * settings page.
 */
export default async function CrmReferralsPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.CrmRead)) {
    return <Alert tone="info">{crmArea.noAccess}</Alert>;
  }

  const customer = typeof params.customer === 'string' && params.customer.trim() ? params.customer.trim() : undefined;
  const cursor = readCursor(params);
  const page = await authed(() => listStaffReferrals({ customer, cursor }));
  const base = crmPaths.referrals();

  return (
    <div className="space-y-5">
      <PageHeader
        title={staffReferralsText.title}
        subtitle={staffReferralsText.subtitle}
        actions={
          context.can(Scope.SettingsWrite) && (
            <Link href={referralPaths.loyaltySettings} className={buttonClass('secondary', 'sm')}>
              {staffReferralsText.settingsLink}
            </Link>
          )
        }
      />
      <div className="space-y-2">
        <p className="text-sm text-ink-600">{staffReferralsText.filterHint}</p>
        <CustomerLookup canSearch={context.can(Scope.CustomerRead)} target="referrals" />
      </div>
      {customer && <ReferralFilterNote customerId={customer} />}
      <ReferralList
        referrals={page.data}
        nextHref={page.page.next_cursor ? withCursor(base, { customer }, page.page.next_cursor) : undefined}
        firstHref={cursor ? withCursor(base, { customer }) : undefined}
      />
    </div>
  );
}

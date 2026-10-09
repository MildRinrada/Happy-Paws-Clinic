import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listCoupons } from '@/features/crm/api/coupons';
import { CouponFilters } from '@/features/crm/components/coupons/coupon-filters';
import { CouponForm } from '@/features/crm/components/coupons/coupon-form';
import { CouponList } from '@/features/crm/components/coupons/coupon-list';
import { couponPage } from '@/features/crm/content/coupon-page';
import { couponListText } from '@/features/crm/content/coupon-list';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { loadServiceOptions } from '@/features/crm/lib/load-service-options';
import { readCouponQuery, withCursor } from '@/features/crm/lib/read-list-query';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: crmArea.metaTitle(couponPage.title) };

/**
 * Discount codes: the form to issue one (`crm:manage`) and the list of those issued (`crm:read`,
 * docs/contract-extensions.md § 25) with the button to switch one off. Staff bound to branches must
 * limit a new code to one of theirs, so for them "every branch" is not offered.
 */
export default async function CrmCouponsPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);
  const canManage = Boolean(context?.can(Scope.CrmManage));
  const canRead = Boolean(context?.can(Scope.CrmRead));

  if (!context || (!canManage && !canRead)) {
    return <Alert tone="info">{crmArea.noAccess}</Alert>;
  }

  const query = readCouponQuery(params);
  const branches = context.branches.map(({ id, name }) => ({ id, name }));
  const [services, page] = await Promise.all([
    canManage ? loadServiceOptions(undefined, context.can(Scope.ServiceManage)) : Promise.resolve([]),
    canRead ? authed(() => listCoupons(query)) : Promise.resolve(null),
  ]);
  const filters = { q: query.q, status: query.status };

  return (
    <div className="space-y-5">
      <PageHeader title={couponPage.title} subtitle={couponPage.subtitle} />
      {canManage && (
        <Card className="max-w-3xl">
          <CardBody>
            <CouponForm
              services={services}
              branches={branches}
              defaultBranchId={context.branchBound ? (context.branch?.id ?? '') : ''}
              allowAllBranches={!context.branchBound}
              today={clinicDay(new Date())}
            />
          </CardBody>
        </Card>
      )}
      {page ? (
        <CouponList
          coupons={page.data}
          branchNames={Object.fromEntries(branches.map((branch) => [branch.id, branch.name]))}
          canManage={canManage}
          filters={<CouponFilters q={query.q} status={query.status} />}
          nextHref={page.page.next_cursor ? withCursor('/crm/coupons', filters, page.page.next_cursor) : undefined}
          firstHref={query.cursor ? withCursor('/crm/coupons', filters) : undefined}
        />
      ) : (
        <p className="text-sm text-ink-600">{couponListText.noRead}</p>
      )}
    </div>
  );
}

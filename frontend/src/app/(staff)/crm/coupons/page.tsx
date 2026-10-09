import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { CouponForm } from '@/features/crm/components/coupons/coupon-form';
import { couponPage } from '@/features/crm/content/coupon-page';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { loadServiceOptions } from '@/features/crm/lib/load-service-options';
import { Scope } from '@/lib/auth/scopes';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: crmArea.metaTitle(couponPage.title) };

/**
 * A discount code for the counter. Staff bound to branches must limit it to one of theirs, so for
 * them "every branch" is not offered and their current branch is picked first.
 */
export default async function CrmCouponsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.CrmManage)) {
    return <Alert tone="info">{crmArea.noManage}</Alert>;
  }

  const services = await loadServiceOptions(undefined, context.can(Scope.ServiceManage));
  const branches = context.branches.map(({ id, name }) => ({ id, name }));

  return (
    <div className="max-w-3xl space-y-5">
      <PageHeader title={couponPage.title} subtitle={couponPage.subtitle} />
      <Card>
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
    </div>
  );
}

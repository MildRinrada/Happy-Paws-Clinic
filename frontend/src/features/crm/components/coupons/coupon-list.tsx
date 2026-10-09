import type { ReactNode } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { couponListText } from '../../content/coupon-list';
import type { Coupon } from '../../types/crm.types';
import { PageLinks } from '../shared/page-links';
import { CouponRow } from './coupon-row';

interface CouponListProps {
  coupons: Coupon[];
  branchNames: Record<string, string>;
  canManage: boolean;
  filters: ReactNode;
  nextHref?: string;
  firstHref?: string;
}

/** The coupons issued, newest first, with the switch-off button for a session that may use it. */
export function CouponList({ coupons, branchNames, canManage, filters, nextHref, firstHref }: CouponListProps) {
  const text = couponListText;
  const columns = text.columns;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <p className="text-sm text-ink-600">{text.hint}</p>
        {filters}
        {coupons.length === 0 ? (
          <EmptyState title={text.empty} />
        ) : (
          <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
            <table className="w-full min-w-[760px] text-sm">
              <thead className="border-b border-ink-100 text-left text-ink-500">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.code}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.discount}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.period}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.used}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.status}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{columns.action}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-ink-100">
                {coupons.map((coupon) => (
                  <CouponRow key={coupon.id} coupon={coupon} branchNames={branchNames} canManage={canManage} />
                ))}
              </tbody>
            </table>
          </div>
        )}
        <PageLinks nextHref={nextHref} firstHref={firstHref} nextLabel={text.next} firstLabel={text.first} />
      </CardBody>
    </Card>
  );
}

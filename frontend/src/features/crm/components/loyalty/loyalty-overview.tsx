import Link from 'next/link';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { loyaltyPage } from '../../content/loyalty-page';
import { crmPaths } from '../../lib/crm-paths';
import type { CustomerLoyalty } from '../../types/crm.types';
import { PackageList } from '../shared/package-list';
import { CouponRedemptionList } from './coupon-redemption-list';
import { PointsBalance } from './points-balance';
import { PointsHistoryTable } from './points-history-table';
import { TierProgress } from './tier-progress';

interface Props {
  loyalty: CustomerLoyalty;
  customerName?: string;
  customerPhone?: string;
  /** The link to the customer's own page, for a session that may open it. */
  canOpenCustomer: boolean;
}

/** One customer's points, level, history, coupons used and packages. */
export function LoyaltyOverview({ loyalty, customerName, customerPhone, canOpenCustomer }: Props) {
  const text = loyaltyPage;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
        <h2 className="font-display text-xl font-semibold text-ink-900">{customerName ?? loyalty.customer_id}</h2>
        {customerPhone && <span className="tabular-nums text-sm text-ink-500">{customerPhone}</span>}
        {canOpenCustomer && (
          <Link href={crmPaths.customer(loyalty.customer_id)} className="text-sm text-brand-700 hover:underline">
            {text.customerLink}
          </Link>
        )}
      </div>

      <Card>
        <CardBody className="grid gap-6 sm:grid-cols-2">
          <PointsBalance loyalty={loyalty} />
          <TierProgress loyalty={loyalty} />
        </CardBody>
      </Card>

      <div className="grid gap-6 lg:grid-cols-[1fr_20rem]">
        <Card>
          <CardHeader title={text.history.heading} aside={<span className="text-xs text-ink-500">{text.history.limit}</span>} />
          <CardBody>
            <PointsHistoryTable entries={loyalty.history} />
          </CardBody>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader title={text.packages.heading} />
            <CardBody>
              <PackageList packages={loyalty.packages} emptyText={text.packages.empty} />
            </CardBody>
          </Card>
          <Card>
            <CardHeader title={text.coupons.heading} />
            <CardBody>
              <CouponRedemptionList redemptions={loyalty.coupon_redemptions} />
            </CardBody>
          </Card>
        </div>
      </div>
    </div>
  );
}

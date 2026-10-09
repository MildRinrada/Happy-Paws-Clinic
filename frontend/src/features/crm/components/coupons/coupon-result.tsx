import { Button } from '@/components/ui/button';
import { formatDay } from '@/features/inventory/lib/format-day';
import { formatBaht } from '@/lib/format/money';
import { couponPage } from '../../content/coupon-page';
import type { BranchOption, Coupon } from '../../types/crm.types';

interface Props {
  coupon: Coupon;
  branches: BranchOption[];
  onAnother: () => void;
}

/** The coupon just made, its code large enough to read out to a customer. */
export function CouponResult({ coupon, branches, onAnother }: Props) {
  const text = couponPage.result;
  const times = (count?: number | null) => (count == null ? text.unlimited : text.times(count));
  const branchName = coupon.branch_id ? branches.find((branch) => branch.id === coupon.branch_id)?.name ?? coupon.branch_id : null;

  return (
    <div className="space-y-4">
      <div role="status" className="space-y-3 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900">
        <p className="font-medium">{text.heading}</p>
        <div>
          <p className="text-xs text-emerald-800">{text.codeLabel}</p>
          <p className="font-mono text-3xl font-semibold tracking-wider text-ink-900 select-all">{coupon.code}</p>
        </div>
        <ul className="space-y-0.5">
          <li className="font-medium">
            {coupon.name}{' '}
            {coupon.percent != null ? text.percentOff(coupon.percent) : text.amountOff(formatBaht(coupon.amount_satang ?? 0))}
          </li>
          <li>{coupon.service_ids.length === 0 ? text.wholeBill : text.someServices(coupon.service_ids.length)}</li>
          <li>{text.limits(times(coupon.max_redemptions), times(coupon.per_customer_limit))}</li>
          <li>{text.period(formatDay(coupon.valid_from), coupon.valid_until ? formatDay(coupon.valid_until) : text.noEnd)}</li>
          <li>{branchName ? text.branch(branchName) : text.allBranches}</li>
        </ul>
      </div>

      <p className="text-sm text-ink-600">{text.howTo}</p>

      <Button size="sm" variant="secondary" onClick={onAnother}>
        {text.another}
      </Button>
    </div>
  );
}

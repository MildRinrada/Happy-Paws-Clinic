import { formatDay } from '@/features/inventory/lib/format-day';
import { formatBaht } from '@/lib/format/money';
import { couponListText } from '../../content/coupon-list';
import type { Coupon } from '../../types/crm.types';
import { CouponStatusBadge } from './coupon-status-badge';
import { DeactivateCouponButton } from './deactivate-coupon-button';

interface CouponRowProps {
  coupon: Coupon;
  branchNames: Record<string, string>;
  /** Whether this session may switch coupons off (`crm:manage`). */
  canManage: boolean;
}

/** One coupon: its code and name, what it takes off, when, how much is used, and where it stands. */
export function CouponRow({ coupon, branchNames, canManage }: CouponRowProps) {
  const text = couponListText;
  const from = formatDay(coupon.valid_from);
  const period = coupon.valid_until ? text.range(from, formatDay(coupon.valid_until)) : text.from(from);
  const discount = coupon.percent != null ? text.percent(coupon.percent) : formatBaht(coupon.amount_satang ?? 0);
  const switchable = canManage && coupon.status !== 'deactivated' && coupon.status !== 'expired';

  return (
    <tr className="align-top">
      <td className="px-4 py-3">
        <span className="block font-mono font-medium text-ink-900">{coupon.code}</span>
        <span className="block text-xs text-ink-500">
          {coupon.name}, {coupon.branch_id ? (branchNames[coupon.branch_id] ?? coupon.branch_id) : text.allBranches}
        </span>
      </td>
      <td className="whitespace-nowrap px-4 py-3 tabular-nums text-ink-800">{discount}</td>
      <td className="whitespace-nowrap px-4 py-3 text-ink-700">{period}</td>
      <td className="whitespace-nowrap px-4 py-3 tabular-nums text-ink-700">
        {text.used(coupon.redeemed_count, coupon.max_redemptions ?? undefined)}
      </td>
      <td className="px-4 py-3">
        <CouponStatusBadge status={coupon.status} />
        {coupon.status === 'deactivated' && (
          <span className="mt-1 block max-w-48 text-xs text-ink-500">{text.deactivatedNote(coupon.deactivation_reason)}</span>
        )}
      </td>
      <td className="px-4 py-3">{switchable && <DeactivateCouponButton id={coupon.id} />}</td>
    </tr>
  );
}

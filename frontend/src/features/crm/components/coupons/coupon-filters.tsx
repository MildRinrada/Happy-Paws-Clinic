import { buttonClass } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { couponListText, couponStatusLabels } from '../../content/coupon-list';
import type { CouponStatus } from '../../types/crm.types';

/** Search and status for the coupon list, a plain GET form so a filtered list can be bookmarked. */
export function CouponFilters({ q, status }: { q?: string; status?: CouponStatus }) {
  const text = couponListText.filters;

  return (
    <form method="get" role="search" aria-label={text.label} className="flex flex-wrap items-end gap-3">
      <label className="block w-52 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.search}</span>
        <Input name="q" type="search" defaultValue={q} maxLength={120} />
      </label>
      <label className="block w-40 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.status}</span>
        <Select name="status" defaultValue={status ?? ''}>
          <option value="">{text.any}</option>
          {(Object.keys(couponStatusLabels) as CouponStatus[]).map((value) => (
            <option key={value} value={value}>
              {couponStatusLabels[value].text}
            </option>
          ))}
        </Select>
      </label>
      <button type="submit" className={buttonClass('secondary', 'md')}>
        {text.apply}
      </button>
    </form>
  );
}

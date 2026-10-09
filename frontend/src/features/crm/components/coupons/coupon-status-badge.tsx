import { Badge } from '@/components/ui/badge';
import { couponStatusLabels } from '../../content/coupon-list';
import type { CouponStatus } from '../../types/crm.types';

/** Where a coupon stands today, as a coloured label. */
export function CouponStatusBadge({ status }: { status: CouponStatus }) {
  const label = couponStatusLabels[status];

  return <Badge tone={label.tone}>{label.text}</Badge>;
}

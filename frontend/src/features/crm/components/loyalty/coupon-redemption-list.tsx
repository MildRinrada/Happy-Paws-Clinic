import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { formatDateTime } from '@/lib/format/datetime';
import { formatBaht } from '@/lib/format/money';
import { loyaltyPage } from '../../content/loyalty-page';
import { crmPaths } from '../../lib/crm-paths';
import type { CouponRedemption } from '../../types/crm.types';

/** The coupons this customer used, newest first. A released one went back when its bill was voided or refunded. */
export function CouponRedemptionList({ redemptions }: { redemptions: CouponRedemption[] }) {
  const text = loyaltyPage.coupons;

  if (redemptions.length === 0) {
    return <p className="text-sm text-ink-500">{text.empty}</p>;
  }

  return (
    <ul className="divide-y divide-ink-100 text-sm">
      {redemptions.map((item) => (
        <li key={item.id} className="flex flex-wrap items-center justify-between gap-2 py-2">
          <span className="flex flex-wrap items-center gap-2">
            <span className="font-mono font-medium text-ink-900">{text.row(item.code, formatBaht(item.amount_satang))}</span>
            {item.released_at && <Badge>{text.released}</Badge>}
          </span>
          <span className="flex flex-wrap items-center gap-3 text-ink-600">
            <Link href={crmPaths.invoice(item.invoice_id)} className="text-brand-700 hover:underline">
              {item.invoice_number ?? item.invoice_id}
            </Link>
            <span>{formatDateTime(item.redeemed_at)}</span>
          </span>
        </li>
      ))}
    </ul>
  );
}

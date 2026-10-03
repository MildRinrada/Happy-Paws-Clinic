import Link from 'next/link';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { formatBaht } from '@/lib/format/money';
import { customerDetail } from '../content/customer-detail';

/**
 * What this customer still owes, so the counter knows to ask before they leave. The link to the
 * cash desk appears only for a session that can open it.
 */
export function BalanceCard({ outstandingSatang, canOpenBilling }: { outstandingSatang: number; canOpenBilling: boolean }) {
  const text = customerDetail.balance;
  const owing = outstandingSatang > 0;

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody className="space-y-2">
        <p className={`font-display text-2xl font-semibold tabular-nums ${owing ? 'text-red-700' : 'text-ink-900'}`}>
          {formatBaht(outstandingSatang)}
        </p>
        {!owing && <p className="text-sm text-ink-500">{text.clear}</p>}
        {owing && canOpenBilling && (
          <Link href="/billing" className="text-sm font-medium text-brand-700 hover:underline">
            {text.open}
          </Link>
        )}
      </CardBody>
    </Card>
  );
}

import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { getLoyalty } from '../../api/getLoyalty';
import { tierLabels, tierTones } from '../../content/crm-labels';
import { customerLoyaltyCard } from '../../content/customer-loyalty-card';
import { crmPaths } from '../../lib/crm-paths';
import type { CustomerLoyalty } from '../../types/crm.types';
import { PackageList } from '../shared/package-list';

/**
 * Points, level and usable packages on the customer's page, for a session with `crm:read`. Reads
 * its own data, and a failed read shows one quiet line instead of costing the customer page.
 */
export async function CustomerLoyaltyCard({ customerId }: { customerId: string }) {
  const text = customerLoyaltyCard;
  let loyalty: CustomerLoyalty | null = null;

  try {
    loyalty = await getLoyalty(customerId);
  } catch {
    loyalty = null;
  }

  const usable = loyalty?.packages.filter((item) => item.status === 'active' || item.status === 'pending_payment') ?? [];

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody className="space-y-3">
        {loyalty ? (
          <>
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-display text-2xl font-semibold tabular-nums text-ink-900">{text.points(loyalty.points_balance)}</span>
              <Badge tone={tierTones[loyalty.tier]}>{tierLabels[loyalty.tier]}</Badge>
            </div>
            <PackageList packages={usable} emptyText={text.noPackages} />
          </>
        ) : (
          <p className="text-sm text-ink-500">{text.failed}</p>
        )}
        <Link href={crmPaths.loyalty(customerId)} className="text-sm font-medium text-brand-700 hover:underline">
          {text.open}
        </Link>
      </CardBody>
    </Card>
  );
}

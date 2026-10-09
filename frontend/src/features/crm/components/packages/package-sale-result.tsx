import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { Button, buttonClass } from '@/components/ui/button';
import { formatBaht } from '@/lib/format/money';
import { packageSalePage } from '../../content/package-sale-page';
import { crmPaths } from '../../lib/crm-paths';
import type { PackageSale } from '../../types/crm.types';
import { PackageStatusBadge } from '../shared/package-status-badge';

interface Props {
  sale: PackageSale;
  canReadLoyalty: boolean;
  onAnother: () => void;
}

/** The package just sold and its bill, with the way to the cash desk to take the money. */
export function PackageSaleResult({ sale, canReadLoyalty, onAnother }: Props) {
  const text = packageSalePage.result;
  const { package: sold, invoice } = sale;

  return (
    <div className="space-y-4">
      <Alert tone="success">
        <p className="flex flex-wrap items-center gap-2 font-medium">
          {text.heading(sold.name)}
          <PackageStatusBadge status={sold.status} />
        </p>
        <p className="mt-1">
          {text.uses(sold.total_uses)} {text.price(formatBaht(sold.price_satang))}
        </p>
        <p className="mt-1">{text.invoice(invoice.number)}</p>
      </Alert>

      {sold.status === 'pending_payment' && <p className="text-sm text-ink-700">{text.pending}</p>}

      <div className="flex flex-wrap items-center gap-3">
        <Link href={crmPaths.invoice(invoice.id)} className={buttonClass('primary', 'sm')}>
          {text.collect}
        </Link>
        {canReadLoyalty && (
          <Link href={crmPaths.loyalty(sold.customer_id)} className="text-sm text-brand-700 hover:underline">
            {text.loyalty}
          </Link>
        )}
        <Button size="sm" variant="ghost" onClick={onAnother}>
          {text.another}
        </Button>
      </div>
    </div>
  );
}

import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { dueBackPreview } from '../../content/due-back-preview';
import type { DueBackPreviewRow as Row } from '../../types/today.types';

/** One customer to call: the name, the pets, the phone number to dial, and how late the vaccine is. */
export function DueBackPreviewRow({ row, canOpenCustomer }: { row: Row; canOpenCustomer: boolean }) {
  const text = dueBackPreview;

  return (
    <li className="flex items-start justify-between gap-3 py-2.5">
      <div className="min-w-0 sm:flex sm:flex-1 sm:items-baseline sm:justify-between sm:gap-3">
        <div className="min-w-0">
          {canOpenCustomer ? (
            <Link href={customerPaths.detail(row.customerId)} className="font-medium text-brand-700 hover:underline">
              {row.customerName}
            </Link>
          ) : (
            <span className="font-medium text-ink-900">{row.customerName}</span>
          )}
          <p className="text-xs text-ink-600">{row.petNames.join(', ')}</p>
        </div>
        {row.phone && (
          <a href={`tel:${row.phone}`} className="text-sm tabular-nums text-brand-700 hover:underline">
            {row.phone}
          </a>
        )}
      </div>
      <Badge tone="amber">{text.overdue(row.daysOverdue)}</Badge>
    </li>
  );
}

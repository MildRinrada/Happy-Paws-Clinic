import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { formatDay } from '@/features/inventory/lib/format-day';
import { dueBackPage } from '../../content/due-back-page';
import { crmPaths } from '../../lib/crm-paths';
import type { DueBackCustomer } from '../../types/due-back.types';
import { DueBackPets } from './due-back-pets';

/** One customer to call: who, which pets, how late, and why the list thinks so. */
export function DueBackRow({ row }: { row: DueBackCustomer }) {
  const text = dueBackPage;
  const lapsed = row.reason === 'lapsed';

  return (
    <li className="space-y-1.5 py-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <Link href={crmPaths.customer(row.customer_id)} className="font-medium text-brand-700 hover:underline">
          {row.customer_name}
        </Link>
        <Badge tone={row.days_overdue > 60 ? 'neutral' : 'amber'}>{text.overdueFor(row.days_overdue)}</Badge>
      </div>
      <DueBackPets pets={row.pets} />
      <p className="text-xs text-ink-600">
        {lapsed ? text.dueBackOn(formatDay(row.due_on)) : text.dueOn(formatDay(row.due_on))}
        <span className="ml-2">{row.last_visit_on ? text.lastVisit(formatDay(row.last_visit_on)) : text.neverVisited}</span>
      </p>
      {lapsed && row.visit_count !== undefined && row.usual_interval_days !== undefined && row.allowed_days !== undefined && (
        <p className="text-xs text-ink-500">{text.rhythm(row.visit_count, row.usual_interval_days, row.allowed_days)}</p>
      )}
    </li>
  );
}

import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { formatLongDate } from '@/lib/format/datetime';
import { waitingListText } from '../../content/advanced-booking';
import type { WaitingListItem } from '../../types/advanced-booking.types';
import { WaitingListRemove } from './waiting-list-remove';

interface WaitingListRowProps {
  entry: WaitingListItem;
  /** Staff id to name, for an entry that waits for one vet. */
  vetNames: Record<string, string>;
  /** `customer:read`, which the owner's page needs. */
  canOpenCustomer: boolean;
}

/** One pet waiting: for what, which days, where, and how often it has been told. */
export function WaitingListRow({ entry, vetNames, canOpenCustomer }: WaitingListRowProps) {
  const text = waitingListText.list;
  // Dates are clinic days; noon keeps the long format on the same day in any viewer's zone.
  const day = (value: string) => formatLongDate(`${value}T12:00:00+07:00`);
  const vet = entry.staff_id ? (vetNames[entry.staff_id] ?? entry.staff_id) : undefined;

  return (
    <li className="space-y-2 py-3 first:pt-0 last:pb-0">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="space-y-0.5">
          <p className="font-semibold text-ink-900">
            {entry.pet_name} <span className="font-normal text-ink-600">/ {entry.service_name}</span>
          </p>
          <p className="text-sm text-ink-700">{text.window(day(entry.earliest_on), day(entry.latest_on))}</p>
        </div>
        <div className="flex flex-wrap items-center gap-1.5">
          <Badge tone={entry.branch_id ? 'brand' : 'neutral'}>{entry.branch_id ? text.thisBranch : text.anyBranch}</Badge>
          {vet && <Badge tone="blue">{text.vet(vet)}</Badge>}
          <Badge tone={entry.notified_count > 0 ? 'amber' : 'neutral'}>{text.notified(entry.notified_count)}</Badge>
        </div>
      </div>

      {entry.note && <p className="text-sm text-ink-600">{entry.note}</p>}

      <div className="flex flex-wrap items-center justify-between gap-2">
        {canOpenCustomer ? (
          <Link href={customerPaths.detail(entry.customer_id)} className="text-sm font-medium text-brand-700 hover:underline">
            {text.owner}
          </Link>
        ) : (
          <span />
        )}
        <WaitingListRemove entryId={entry.id} petName={entry.pet_name} />
      </div>
    </li>
  );
}

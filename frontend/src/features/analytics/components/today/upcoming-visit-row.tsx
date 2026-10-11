import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { appointmentStatusLabel } from '@/features/appointments/lib/status-labels';
import { formatTime } from '@/lib/format/datetime';
import { upcomingVisits } from '../../content/upcoming-visits';
import type { UpcomingVisit } from '../../types/today.types';

/**
 * One appointment starting soon: when, which pet, whose, and where it stands. There is no page for
 * a single appointment, so the line opens today's calendar, where the appointment is acted on.
 */
export function UpcomingVisitRow({ visit, href }: { visit: UpcomingVisit; href: string }) {
  const status = appointmentStatusLabel[visit.status];

  return (
    <li>
      <Link href={href} className="flex items-center gap-3 rounded-lg px-3 py-2 transition-colors hover:bg-ink-50 focus-visible:bg-ink-50">
        <span className="w-12 shrink-0 text-sm font-semibold tabular-nums text-ink-900">{formatTime(visit.startsAt)}</span>
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-medium text-ink-900">{visit.petName ?? upcomingVisits.unknownPet}</span>
          {visit.customerName && <span className="block truncate text-xs text-ink-600">{visit.customerName}</span>}
        </span>
        <Badge tone={status.tone}>{status.text}</Badge>
      </Link>
    </li>
  );
}

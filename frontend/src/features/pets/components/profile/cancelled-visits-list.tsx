import Link from 'next/link';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import { clinicDay, formatDateTime } from '@/lib/format/datetime';
import { undoDeath } from '../../content/undo-death';
import type { CancelledVisit } from '../../types/pet.types';

/**
 * The visits the death cancelled, still cancelled. Each future one links to booking on its day;
 * the counter rebooks with the owner rather than the system guessing.
 */
export function CancelledVisitsList({ visits }: { visits: CancelledVisit[] }) {
  const text = undoDeath;

  if (visits.length === 0) return <p className="text-sm text-ink-500">{text.noVisits}</p>;

  return (
    <Card>
      <CardHeader title={text.visitsTitle(visits.length)} />
      <CardBody>
        <p className="mb-3 text-sm text-ink-500">{text.visitsHint}</p>
        <ul className="space-y-2 text-sm">
          {visits.map((visit) => (
            <li key={visit.id} className="flex items-center justify-between gap-3">
              <span className="text-ink-900">
                {formatDateTime(visit.starts_at)}
                <span className="ml-2 font-mono text-xs text-ink-500">{visit.id}</span>
              </span>
              {visit.has_passed ? (
                <span className="text-ink-500">{text.passed}</span>
              ) : (
                <Link href={appointmentPaths.book(clinicDay(visit.starts_at))} className="font-medium text-brand-700 hover:underline">
                  {text.rebook}
                </Link>
              )}
            </li>
          ))}
        </ul>
      </CardBody>
    </Card>
  );
}

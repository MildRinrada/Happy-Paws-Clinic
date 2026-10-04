import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import { clinicDay, formatDateTime, formatTime } from '@/lib/format/datetime';
import { schedulePage } from '../content/schedule-page';
import type { TimeOffResult as Result } from '../types/schedule.types';

interface TimeOffResultProps {
  result: Result;
  /** Staff id to name, for the suggested substitutes. */
  names: Record<string, string>;
}

/** What the leave did: shifts taken off, and each appointment someone now has to call about. */
export function TimeOffResult({ result, names }: TimeOffResultProps) {
  const text = schedulePage.timeOff;

  return (
    <div className="space-y-2">
      <Alert tone="success">{text.result(result.shifts_cancelled)}</Alert>

      {result.conflicts.length === 0 ? (
        <p className="text-sm text-ink-600">{text.noConflicts}</p>
      ) : (
        <Alert tone="error">
          <p className="font-medium">{text.conflicts}</p>
          <ul className="mt-1.5 space-y-1">
            {result.conflicts.map((conflict) => {
              const substitutes = conflict.suggested_staff_ids.map((id) => names[id] ?? id);

              return (
                <li key={conflict.appointment_id}>
                  {formatDateTime(conflict.starts_at)}-{formatTime(conflict.ends_at)}{' '}
                  <span className="text-red-900/80">
                    {text.suggested}: {substitutes.length > 0 ? substitutes.join(', ') : text.nobody}
                  </span>{' '}
                  <Link href={appointmentPaths.calendar(clinicDay(conflict.starts_at))} className="underline">
                    {text.openAppointment}
                  </Link>
                </li>
              );
            })}
          </ul>
        </Alert>
      )}
    </div>
  );
}

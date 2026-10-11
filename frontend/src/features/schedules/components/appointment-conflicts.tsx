import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import { clinicDay, formatDateTime, formatTime } from '@/lib/format/datetime';
import { schedulePage } from '../content/schedule-page';
import type { TimeOffConflict } from '../types/schedule.types';

interface AppointmentConflictsProps {
  conflicts: TimeOffConflict[];
  /** Staff id to name, for the suggested substitutes. */
  names: Record<string, string>;
  /** What the list is: appointments in the leave, or in the time a shift no longer covers. */
  heading: string;
  /** Said when there are none. */
  emptyText: string;
}

/**
 * Appointments already booked in time someone is no longer rostered for, each with who could
 * take it and a link to that day's calendar. Leave and shift changes answer the same list.
 */
export function AppointmentConflicts({ conflicts, names, heading, emptyText }: AppointmentConflictsProps) {
  const text = schedulePage.timeOff;

  if (conflicts.length === 0) {
    return <p className="text-sm text-ink-600">{emptyText}</p>;
  }

  return (
    <Alert tone="error">
      <p className="font-medium">{heading}</p>
      <ul className="mt-1.5 space-y-1">
        {conflicts.map((conflict) => {
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
  );
}

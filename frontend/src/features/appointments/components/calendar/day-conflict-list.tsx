import { formatTime } from '@/lib/format/datetime';
import { conflictKindLabel, dayConflictText } from '../../content/advanced-booking';
import type { AppointmentConflict } from '../../types/advanced-booking.types';

interface DayConflictListProps {
  conflicts: AppointmentConflict[];
  /** Appointment id to the line a person recognises it by (time, pet, vet). */
  labels: Record<string, string>;
}

/** One line per problem: which booked visit, what kind, and the backend's own sentence. */
export function DayConflictList({ conflicts, labels }: DayConflictListProps) {
  const text = dayConflictText;

  return (
    <ul className="divide-y divide-amber-200/70 text-sm">
      {conflicts.map((conflict, at) => {
        const label = conflict.appointment_id
          ? (labels[conflict.appointment_id] ?? text.unknownAppointment)
          : text.window(formatTime(conflict.starts_at), formatTime(conflict.ends_at));

        return (
          <li key={`${conflict.appointment_id ?? conflict.kind}-${at}`} className="space-y-0.5 py-2 first:pt-0 last:pb-0">
            <p className="font-medium text-ink-900">{label}</p>
            <p className="text-amber-900">
              <span className="font-medium">{conflictKindLabel[conflict.kind] ?? conflict.kind}</span>
              {conflict.message && <span className="text-amber-800">: {conflict.message}</span>}
            </p>
          </li>
        );
      })}
    </ul>
  );
}

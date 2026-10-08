import { conflictKindLabel } from '../../content/advanced-booking';
import type { AppointmentConflict } from '../../types/advanced-booking.types';

/** The problems found at one time: what kind, and the backend's own sentence. */
export function ConflictLines({ conflicts }: { conflicts: AppointmentConflict[] }) {
  return (
    <ul className="space-y-0.5 text-sm text-amber-900">
      {conflicts.map((conflict, at) => (
        <li key={`${conflict.kind}-${at}`}>
          <span className="font-medium">{conflictKindLabel[conflict.kind] ?? conflict.kind}</span>
          {conflict.message && <span className="text-amber-800">: {conflict.message}</span>}
        </li>
      ))}
    </ul>
  );
}

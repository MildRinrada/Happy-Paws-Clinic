import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { portalReschedule } from '../../content/portal-reschedule';
import type { RescheduleView } from '../../lib/load-reschedule';

/** The appointment as it stands: what, with whom, and the time it is moving from. */
export function CurrentAppointment({ appointment }: { appointment: RescheduleView }) {
  return (
    <div className="flex items-start justify-between gap-4 rounded-xl bg-site-soft px-4 py-3">
      <div className="min-w-0">
        <p className="text-xs font-semibold text-site-muted">{portalReschedule.current}</p>
        <p className="text-sm font-semibold text-ink-900">{appointment.title}</p>
        <p className="text-sm text-ink-700">{appointment.vet}</p>
      </div>
      <div className="shrink-0 text-right">
        <p className="text-xs text-site-muted">{formatLongDate(appointment.startsAt)}</p>
        <p className="font-display text-xl font-bold text-ink-900">{formatTime(appointment.startsAt)}</p>
      </div>
    </div>
  );
}

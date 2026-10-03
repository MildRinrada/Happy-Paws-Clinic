import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { portalAppointments } from '../../content/portal-appointments';
import type { AppointmentRowView } from '../../lib/load-appointments';

const statusTone: Record<string, string> = {
  booked: 'bg-amber-50 text-amber-800',
  confirmed: 'bg-brand-50 text-site-teal',
  checked_in: 'bg-sky-50 text-sky-800',
  in_consultation: 'bg-sky-50 text-sky-800',
  completed: 'bg-ink-100 text-ink-700',
  cancelled: 'bg-red-50 text-red-700',
  no_show: 'bg-red-50 text-red-700',
};

/** One appointment: the time on the left, what and with whom, and its state. */
export function AppointmentRow({ row }: { row: AppointmentRowView }) {
  return (
    <li className="flex items-start gap-4 py-3">
      <div className="w-16 shrink-0 text-center">
        <p className="font-display text-lg font-bold text-ink-900">{formatTime(row.startsAt)}</p>
      </div>

      <div className="min-w-0 flex-1">
        <p className="text-sm font-semibold text-ink-900">{row.title}</p>
        <p className="text-sm text-ink-700">{row.vet}</p>
        <p className="text-xs text-site-muted">{formatLongDate(row.startsAt)}</p>
      </div>

      <span className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${statusTone[row.status] ?? 'bg-ink-100 text-ink-700'}`}>
        {portalAppointments.status[row.status]}
      </span>
    </li>
  );
}

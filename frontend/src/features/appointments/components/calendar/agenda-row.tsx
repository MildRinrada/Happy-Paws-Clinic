import { formatTime } from '@/lib/format/datetime';
import { appointmentCalendar } from '../../content/appointment-calendar';
import type { CalendarItem } from '../../types/calendar.types';
import { AppointmentStatusBadge } from '../appointment-status-badge';

/** One appointment as a row: time, pet, service and vet, with its state. */
export function AgendaRow({ item, onOpen }: { item: CalendarItem; onOpen: () => void }) {
  const { appointment } = item;
  const text = appointmentCalendar;

  return (
    <li>
      <button type="button" onClick={onOpen} className="flex w-full items-start gap-3 px-4 py-3 text-left hover:bg-ink-50">
        <span className="w-12 shrink-0 pt-0.5 text-sm font-semibold tabular-nums text-ink-900">{formatTime(appointment.starts_at)}</span>
        <span className="min-w-0 flex-1">
          <span className="block font-medium text-ink-900">
            {item.petName ?? text.unnamedPet}
            {item.allergies.length > 0 && <span className="ml-2 text-xs font-semibold text-red-700">{text.allergy}</span>}
          </span>
          <span className="block truncate text-sm text-ink-600">
            {[item.serviceName ?? text.unnamedService, item.vetName ?? text.noVet].join(' / ')}
          </span>
        </span>
        <AppointmentStatusBadge status={appointment.status} />
      </button>
    </li>
  );
}

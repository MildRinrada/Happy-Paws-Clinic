import { appointmentCalendar } from '../../content/appointment-calendar';
import type { CalendarItem } from '../../types/calendar.types';
import { AgendaRow } from './agenda-row';

/** The day in time order, for a narrow screen where a grid of vets does not fit. */
export function AgendaList({ items, onOpen }: { items: CalendarItem[]; onOpen: (appointmentId: string) => void }) {
  return (
    <ul aria-label={appointmentCalendar.listLabel} className="divide-y divide-ink-100 rounded-xl border border-ink-200 bg-white">
      {items.map((item) => (
        <AgendaRow key={item.appointment.id} item={item} onOpen={() => onOpen(item.appointment.id)} />
      ))}
    </ul>
  );
}

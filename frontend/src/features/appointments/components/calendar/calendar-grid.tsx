import { appointmentCalendar } from '../../content/appointment-calendar';
import type { CalendarView } from '../../types/calendar.types';
import { TimeAxis } from './time-axis';
import { VetColumn } from './vet-column';

/**
 * The day as a grid: time down the side, one column per vet. Wide screens only; a phone gets the
 * same appointments as a list. Scrolls sideways inside its card when there are many vets.
 */
export function CalendarGrid({ view, onOpen }: { view: CalendarView; onOpen: (appointmentId: string) => void }) {
  const columns = view.columns;
  const template = `repeat(${columns.length}, minmax(170px, 1fr))`;

  return (
    <section aria-label={appointmentCalendar.gridLabel} className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
      <div className="min-w-fit">
        <div className="sticky top-0 z-20 flex border-b border-ink-200 bg-white">
          <div className="w-14 shrink-0" />
          <div className="grid flex-1" style={{ gridTemplateColumns: template }}>
            {columns.map((column) => (
              <p key={column.staffId ?? 'none'} className="truncate border-l border-ink-100 px-3 py-2.5 text-sm font-semibold text-ink-800">
                {column.name}
              </p>
            ))}
          </div>
        </div>

        <div className="flex py-3">
          <TimeAxis start={view.startMinute} end={view.endMinute} />
          <div className="grid flex-1" style={{ gridTemplateColumns: template }}>
            {columns.map((column) => (
              <VetColumn
                key={column.staffId ?? 'none'}
                items={view.items.filter((item) => (item.appointment.staff_id ?? null) === column.staffId)}
                start={view.startMinute}
                end={view.endMinute}
                onOpen={onOpen}
              />
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

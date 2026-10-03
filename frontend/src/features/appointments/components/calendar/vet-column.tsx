import { placeColumn } from '../../lib/block-layout';
import { hoursBetween, offsetPx, PX_PER_MINUTE } from '../../lib/grid-scale';
import type { CalendarItem } from '../../types/calendar.types';
import { AppointmentBlock } from './appointment-block';

interface VetColumnProps {
  items: CalendarItem[];
  start: number;
  end: number;
  onOpen: (appointmentId: string) => void;
}

/** One vet's day: hour lines behind, appointments on top. */
export function VetColumn({ items, start, end, onOpen }: VetColumnProps) {
  return (
    <div className="relative border-l border-ink-100" style={{ height: offsetPx(end, start) }}>
      {hoursBetween(start, end).map((minute) => (
        <div key={minute} aria-hidden="true" className="absolute inset-x-0 border-t border-ink-100" style={{ top: offsetPx(minute, start) }}>
          <div className="border-t border-dashed border-ink-100/70" style={{ marginTop: 30 * PX_PER_MINUTE }} />
        </div>
      ))}

      {placeColumn(items).map((placed) => (
        <AppointmentBlock
          key={placed.item.appointment.id}
          placed={placed}
          gridStart={start}
          onOpen={() => onOpen(placed.item.appointment.id)}
        />
      ))}
    </div>
  );
}

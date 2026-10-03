import { appointmentCalendar } from '../../content/appointment-calendar';
import type { PlacedItem } from '../../lib/block-layout';
import { offsetPx, PX_PER_MINUTE } from '../../lib/grid-scale';
import { appointmentBlockTone } from '../../lib/status-labels';
import { formatTime } from '@/lib/format/datetime';

interface AppointmentBlockProps {
  placed: PlacedItem;
  gridStart: number;
  onOpen: () => void;
}

/** One appointment on the grid, sized to its length. Pressing it opens the details. */
export function AppointmentBlock({ placed, gridStart, onOpen }: AppointmentBlockProps) {
  const { item, start, end, lane, lanes } = placed;
  const { appointment } = item;
  const text = appointmentCalendar;
  const width = 100 / lanes;

  return (
    <button
      type="button"
      onClick={onOpen}
      className={`absolute overflow-hidden rounded-lg border px-2 py-1 text-left text-xs shadow-sm transition hover:z-10 hover:shadow-md focus-visible:z-10 ${appointmentBlockTone[appointment.status]}`}
      style={{
        top: offsetPx(start, gridStart) + 1,
        height: Math.max((end - start) * PX_PER_MINUTE - 2, 22),
        left: `calc(${lane * width}% + 2px)`,
        width: `calc(${width}% - 4px)`,
      }}
    >
      <span className="flex items-center gap-1 font-semibold text-ink-900">
        <span className="tabular-nums">{formatTime(appointment.starts_at)}</span>
        <span className="truncate">{item.petName ?? text.unnamedPet}</span>
        {item.allergies.length > 0 && (
          <span
            className="rounded bg-red-600 px-1 text-[10px] font-semibold text-white"
            title={text.allergyList(item.allergies)}
          >
            !
          </span>
        )}
      </span>
      <span className="block truncate text-ink-600">{item.serviceName ?? text.unnamedService}</span>
    </button>
  );
}

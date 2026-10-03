import { timeOf } from '../../lib/calendar-day';
import { hoursBetween, offsetPx } from '../../lib/grid-scale';

/** The hour labels down the left of the grid. */
export function TimeAxis({ start, end }: { start: number; end: number }) {
  return (
    <div aria-hidden="true" className="relative w-14 shrink-0" style={{ height: offsetPx(end, start) }}>
      {hoursBetween(start, end).map((minute) => (
        <span
          key={minute}
          className="absolute right-2 -translate-y-1/2 text-xs tabular-nums text-ink-500 first:translate-y-0"
          style={{ top: offsetPx(minute, start) }}
        >
          {timeOf(minute)}
        </span>
      ))}
    </div>
  );
}

import { bookingFrame } from '../../content/booking-frame';
import { dayChipParts, dayOfMonth } from '../../lib/booking-dates';
import type { DayStatus } from '../../types/booking.types';

interface DayChipProps {
  day: string;
  isToday: boolean;
  status: DayStatus;
  selected: boolean;
  onSelect: (day: string) => void;
}

/**
 * One day: its weekday, the date in large, and "วันนี้", "ว่าง" or "เต็ม". The chosen day is dark
 * green. A full day is muted and cannot be chosen, unless it is the one chosen already (a link
 * can open on one), so the times under it still explain themselves.
 */
export function DayChip({ day, isToday, status, selected, onSelect }: DayChipProps) {
  const text = bookingFrame.time;
  const { weekday, date } = dayChipParts(day);
  const full = status === 'full';
  const word = full ? text.full : status === 'free' ? (isToday ? text.today : text.free) : '';

  const look = selected
    ? 'border-site-forest bg-site-forest text-white'
    : full
      ? 'border-ink-200 bg-white text-ink-400'
      : 'border-ink-200 bg-white text-site-forest hover:border-site-forest';

  return (
    <button
      type="button"
      onClick={() => onSelect(day)}
      disabled={full && !selected}
      aria-pressed={selected}
      aria-label={text.dayLabel(`${weekday} ${date}`, word)}
      className={`flex h-[87px] w-[68px] shrink-0 flex-col items-center justify-center rounded-[11px] border transition disabled:cursor-not-allowed sm:w-full ${look}`}
    >
      <span className="text-xs">{weekday}</span>
      <span className="text-[21px] font-bold leading-tight">{dayOfMonth(day)}</span>
      {/* Kept in place while loading, so the chips do not grow when the words arrive. */}
      <span className={`min-h-4 text-xs ${selected ? 'text-white' : full ? 'text-ink-400' : 'text-ink-700'}`}>{word}</span>
    </button>
  );
}

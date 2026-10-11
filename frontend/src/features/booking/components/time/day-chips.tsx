import type { DayStatus } from '../../types/booking.types';
import { DayChip } from './day-chip';

interface DayChipsProps {
  days: string[];
  today: string;
  value: string;
  statusOf: (day: string) => DayStatus;
  onSelect: (day: string) => void;
}

/** Seven day chips in a row; on a phone the row scrolls sideways instead of squeezing them. */
export function DayChips({ days, today, value, statusOf, onSelect }: DayChipsProps) {
  return (
    <ul className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1 sm:grid sm:grid-cols-7 sm:overflow-visible">
      {days.map((day) => (
        <li key={day} className="flex">
          <DayChip day={day} isToday={day === today} status={statusOf(day)} selected={day === value} onSelect={onSelect} />
        </li>
      ))}
    </ul>
  );
}

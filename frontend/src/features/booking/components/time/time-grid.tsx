import type { SlotTime } from '../../lib/slot-times';
import { TimeButton } from './time-button';

interface TimeGridProps {
  times: SlotTime[];
  /** The time picked, as the instant the API sent or the link asked for. */
  value?: string;
  disabled: boolean;
  onSelect: (time: SlotTime) => void;
}

/** The day's free times, six to a row (three on a phone). */
export function TimeGrid({ times, value, disabled, onSelect }: TimeGridProps) {
  const picked = value ? Date.parse(value) : undefined;

  return (
    <ul className="grid grid-cols-3 gap-2 sm:grid-cols-6">
      {times.map((time) => (
        <li key={time.startsAt}>
          <TimeButton
            startsAt={time.startsAt}
            selected={Date.parse(time.startsAt) === picked}
            disabled={disabled}
            onSelect={() => onSelect(time)}
          />
        </li>
      ))}
    </ul>
  );
}

import { Alert } from '@/components/ui/alert';
import { copyWeekText } from '../../content/copy-week';
import { blockHours, dayLabel } from '../../lib/chip-label';
import { clinicDate } from '../../lib/clinic-clock';
import type { ShiftClash } from '../../types/schedule.types';

interface CopyWeekClashesProps {
  /** What was in the way: shifts already there, or leave. */
  clashes: ShiftClash[];
  /** Staff id to name. */
  names: Record<string, string>;
}

/** Why nothing was copied: every shift or leave in the target week a copy would have landed on, with whose it is. */
export function CopyWeekClashes({ clashes, names }: CopyWeekClashesProps) {
  const text = copyWeekText;

  return (
    <Alert tone="error">
      <p className="font-medium">{text.clashes}</p>
      <ul className="mt-1.5 space-y-1">
        {clashes.map((clash) => (
          <li key={clash.shift_id ?? clash.time_off_id ?? clash.starts_at}>
            {(clash.staff_id && names[clash.staff_id]) ?? clash.staff_id}{' '}
            <span className="text-red-900/80">
              {clash.time_off_id ? text.clashLeave : text.clashShift} {dayLabel(clinicDate(clash.starts_at))} {blockHours(clash)}
            </span>
          </li>
        ))}
      </ul>
    </Alert>
  );
}

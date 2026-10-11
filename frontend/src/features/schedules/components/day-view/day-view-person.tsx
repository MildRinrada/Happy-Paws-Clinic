import { Badge } from '@/components/ui/badge';
import { schedulePage } from '../../content/schedule-page';
import type { RosterRow } from '../../lib/roster-rows';
import { touchesDay } from '../../lib/roster-week';
import { shiftChips } from '../../lib/shift-chips';
import { LeaveChip } from '../leave-chip';
import { RosterShiftChip } from '../roster-shift-chip';

interface DayViewPersonProps {
  row: RosterRow;
  day: string;
  /** Now, in milliseconds, read once for the page. */
  now: number;
}

/** One person working the picked day on a phone: name on the left, their shifts and leave on the right. */
export function DayViewPerson({ row, day, now }: DayViewPersonProps) {
  const chips = shiftChips(row.shifts, day);
  const leave = row.time_off.filter((entry) => touchesDay(entry.starts_at, entry.ends_at, day));

  return (
    <li className="flex items-start justify-between gap-3 px-4 py-2.5">
      <span className="min-w-0 text-sm font-medium text-ink-900">
        <span className="block truncate">{row.display_name}</span>
        {row.isMe && <Badge tone="brand">{schedulePage.me}</Badge>}
      </span>
      <ul className="w-40 shrink-0 space-y-1">
        {chips.map((chip) => (
          <li key={chip.key}>
            <RosterShiftChip
              chip={chip}
              staffId={row.staff_id}
              name={row.display_name}
              day={day}
              editable={Date.parse(chip.endsAt) > now}
            />
          </li>
        ))}
        {leave.map((entry) => (
          <li key={entry.id}>
            <LeaveChip entry={entry} />
          </li>
        ))}
      </ul>
    </li>
  );
}

import { shiftChips } from '../lib/shift-chips';
import { touchesDay } from '../lib/roster-week';
import type { StaffSchedule } from '../types/schedule.types';
import { EmptyCellButton } from './empty-cell-button';
import { LeaveChip } from './leave-chip';
import { RosterShiftChip } from './roster-shift-chip';

interface RosterCellProps {
  person: StaffSchedule;
  day: string;
  /** Today in clinic time: its column is shaded, and no shift is offered before it. */
  today: string;
  /** Now, in milliseconds, read once for the page: shifts already over are not offered for change. */
  now: number;
}

/** One person's day: each shift, leave when any of the day is taken, and "+" when the day is still open. */
export function RosterCell({ person, day, today, now }: RosterCellProps) {
  const chips = shiftChips(person.shifts, day);
  const leave = person.time_off.filter((entry) => touchesDay(entry.starts_at, entry.ends_at, day));

  return (
    <td className={`border-l border-ink-100 px-1.5 py-1.5 align-top ${day === today ? 'bg-brand-50/50' : ''}`}>
      <ul className="space-y-1">
        {chips.map((chip) => (
          <li key={chip.key}>
            <RosterShiftChip
              chip={chip}
              staffId={person.staff_id}
              name={person.display_name}
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
      {chips.length === 0 && day >= today && <EmptyCellButton staffId={person.staff_id} name={person.display_name} day={day} />}
    </td>
  );
}

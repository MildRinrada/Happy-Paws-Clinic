import { formatTime } from '@/lib/format/datetime';
import { schedulePage } from '../content/schedule-page';
import { touchesDay } from '../lib/roster-week';
import type { StaffSchedule } from '../types/schedule.types';

/** One person's day: each working block, and leave when any of the day is taken. */
export function RosterCell({ person, day }: { person: StaffSchedule; day: string }) {
  const shifts = person.shifts.filter((shift) => touchesDay(shift.starts_at, shift.ends_at, day));
  const leave = person.time_off.filter((entry) => touchesDay(entry.starts_at, entry.ends_at, day));

  return (
    <td className="border-l border-ink-100 px-2 py-2 align-top">
      <ul className="space-y-1">
        {shifts.map((shift) => (
          <li key={shift.id} className="whitespace-nowrap rounded-md bg-brand-50 px-1.5 py-0.5 text-xs tabular-nums text-brand-800">
            {formatTime(shift.starts_at)}-{formatTime(shift.ends_at)}
          </li>
        ))}
        {leave.map((entry) => (
          <li key={entry.id} title={entry.reason} className="rounded-md bg-amber-100 px-1.5 py-0.5 text-xs text-amber-900">
            {schedulePage.leave}
            {entry.reason ? ` · ${entry.reason}` : ''}
          </li>
        ))}
      </ul>
    </td>
  );
}

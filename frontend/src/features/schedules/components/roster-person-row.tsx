import { Badge } from '@/components/ui/badge';
import { schedulePage } from '../content/schedule-page';
import type { RosterRow } from '../lib/roster-rows';
import { workedHours } from '../lib/roster-summary';
import { RosterCell } from './roster-cell';

interface RosterPersonRowProps {
  row: RosterRow;
  days: string[];
  today: string;
  now: number;
}

/** One person's week: their name, a cell per day, and their hours for the week, breaks left out. */
export function RosterPersonRow({ row, days, today, now }: RosterPersonRowProps) {
  const hours = workedHours(row.shifts);

  return (
    <tr className={row.isMe ? 'bg-brand-50/30' : undefined}>
      <th scope="row" className="min-w-44 px-3 py-2 align-top font-medium text-ink-900">
        <span className="block">{row.display_name}</span>
        {row.isMe && <Badge tone="brand">{schedulePage.me}</Badge>}
      </th>
      {days.map((day) => (
        <RosterCell key={day} person={row} day={day} today={today} now={now} />
      ))}
      <td className="whitespace-nowrap border-l border-ink-100 px-3 py-2 text-right align-top tabular-nums text-ink-700">
        {hours > 0 ? schedulePage.hoursValue(hours) : '-'}
      </td>
    </tr>
  );
}

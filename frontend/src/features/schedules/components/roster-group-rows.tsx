import { schedulePage } from '../content/schedule-page';
import type { RosterGroup } from '../lib/roster-rows';
import { RosterPersonRow } from './roster-person-row';

interface RosterGroupRowsProps {
  group: RosterGroup;
  days: string[];
  today: string;
  now: number;
}

/** One department's rows under a heading that names it and says how many are in it. */
export function RosterGroupRows({ group, days, today, now }: RosterGroupRowsProps) {
  return (
    <tbody className="divide-y divide-ink-100 border-t border-ink-200">
      <tr className="bg-ink-50/80">
        <th scope="colgroup" colSpan={days.length + 2} className="px-3 py-1.5 text-xs font-semibold text-ink-600">
          {schedulePage.groups[group.key]} ({group.rows.length})
        </th>
      </tr>
      {group.rows.map((row) => (
        <RosterPersonRow key={row.staff_id} row={row} days={days} today={today} now={now} />
      ))}
    </tbody>
  );
}

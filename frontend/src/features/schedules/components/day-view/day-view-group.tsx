import { Card, CardHeader } from '@/components/ui/card';
import { schedulePage } from '../../content/schedule-page';
import type { RosterGroup } from '../../lib/roster-rows';
import { DayViewPerson } from './day-view-person';

interface DayViewGroupProps {
  group: RosterGroup;
  day: string;
  now: number;
}

/** One department's people on the picked day, as a card. */
export function DayViewGroup({ group, day, now }: DayViewGroupProps) {
  return (
    <Card>
      <CardHeader title={`${schedulePage.groups[group.key]} (${group.rows.length})`} />
      <ul className="divide-y divide-ink-100">
        {group.rows.map((row) => (
          <DayViewPerson key={row.staff_id} row={row} day={day} now={now} />
        ))}
      </ul>
    </Card>
  );
}

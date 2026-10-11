import { Card } from '@/components/ui/card';
import { schedulePage } from '../content/schedule-page';
import type { RosterGroup } from '../lib/roster-rows';
import type { DayCover } from '../lib/roster-summary';
import { RosterDayHeader } from './roster-day-header';
import { RosterGroupRows } from './roster-group-rows';
import { RosterSummaryRow } from './roster-summary-row';

interface RosterTableProps {
  days: string[];
  groups: RosterGroup[];
  /** Today in clinic time, to shade its column. */
  today: string;
  /** Now, in milliseconds, read once for the page. */
  now: number;
  cover: Record<string, DayCover>;
  /** Days the demand forecast expects to be heavy. */
  busy: Set<string>;
}

/**
 * The week as a grid on a wide screen: one row per person in department groups, one column per
 * day, each person's hours at the end and who is on at the bottom. Phones get the day view instead.
 */
export function RosterTable({ days, groups, today, now, cover, busy }: RosterTableProps) {
  const text = schedulePage;

  return (
    <Card className="hidden overflow-x-auto md:block">
      <table className="w-full min-w-[58rem] text-left text-sm">
        <thead className="bg-ink-50/60">
          <tr>
            <th scope="col" className="px-3 py-2 align-top font-medium text-ink-600">
              {text.person}
            </th>
            {days.map((day) => (
              <RosterDayHeader key={day} day={day} isToday={day === today} cover={cover[day]!} busy={busy.has(day)} />
            ))}
            <th scope="col" title={text.hoursHint} className="border-l border-ink-100 px-3 py-2 text-right align-top font-medium text-ink-600">
              {text.hours}
            </th>
          </tr>
        </thead>
        {groups.map((group) => (
          <RosterGroupRows key={group.key} group={group} days={days} today={today} now={now} />
        ))}
        <RosterSummaryRow days={days} today={today} cover={cover} />
      </table>
    </Card>
  );
}

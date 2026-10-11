'use client';

import { useState } from 'react';
import { Card } from '@/components/ui/card';
import { schedulePage } from '../../content/schedule-page';
import type { RosterGroup } from '../../lib/roster-rows';
import type { DayCover } from '../../lib/roster-summary';
import { touchesDay } from '../../lib/roster-week';
import { CoverBadges } from '../cover-badges';
import { DayChips } from './day-chips';
import { DayViewGroup } from './day-view-group';

interface RosterDayViewProps {
  days: string[];
  today: string;
  now: number;
  groups: RosterGroup[];
  cover: Record<string, DayCover>;
  /** Days the demand forecast expects to be heavy. */
  busy: string[];
}

/**
 * The roster on a phone: pick a day, then see who works it in their department groups, instead
 * of a seven-column table scrolled sideways. Starts on today when today is in the week.
 */
export function RosterDayView({ days, today, now, groups, cover, busy }: RosterDayViewProps) {
  const [day, setDay] = useState(days.includes(today) ? today : days[0]!);
  const working = groups
    .map((group) => ({
      ...group,
      rows: group.rows.filter((row) =>
        [...row.shifts, ...row.time_off].some((entry) => touchesDay(entry.starts_at, entry.ends_at, day)),
      ),
    }))
    .filter((group) => group.rows.length > 0);

  return (
    <section aria-label={schedulePage.dayView.pick} className="space-y-3 md:hidden">
      <DayChips days={days} today={today} selected={day} onSelect={setDay} />
      <CoverBadges cover={cover[day]!} busy={busy.includes(day)} withFrontDesk />
      {working.length === 0 ? (
        <Card>
          <p className="px-4 py-5 text-sm text-ink-500">{schedulePage.dayView.nobody}</p>
        </Card>
      ) : (
        working.map((group) => <DayViewGroup key={group.key} group={group} day={day} now={now} />)
      )}
    </section>
  );
}

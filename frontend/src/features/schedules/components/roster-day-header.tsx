import { dayChipParts } from '@/features/booking/lib/booking-dates';
import { schedulePage } from '../content/schedule-page';
import type { DayCover } from '../lib/roster-summary';
import { CoverBadges } from './cover-badges';

interface RosterDayHeaderProps {
  day: string;
  isToday: boolean;
  cover: DayCover;
  busy: boolean;
}

/** A day's column heading: the date, "วันนี้" on today, the vets on, and the forecast's warning. */
export function RosterDayHeader({ day, isToday, cover, busy }: RosterDayHeaderProps) {
  const { weekday, date } = dayChipParts(day);

  return (
    <th
      scope="col"
      className={`border-l border-ink-100 px-2 py-2 align-top font-medium ${isToday ? 'bg-brand-50 text-brand-800' : 'text-ink-600'}`}
    >
      <span className="block whitespace-nowrap">
        {weekday} {date}
      </span>
      {isToday && <span className="block text-[11px] font-semibold text-brand-700">{schedulePage.today}</span>}
      <span className="mt-1 block">
        <CoverBadges cover={cover} busy={busy} />
      </span>
    </th>
  );
}

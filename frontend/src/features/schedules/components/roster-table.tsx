import { Card } from '@/components/ui/card';
import { dayChipParts } from '@/features/booking/lib/booking-dates';
import { schedulePage } from '../content/schedule-page';
import type { StaffSchedule } from '../types/schedule.types';
import { RosterCell } from './roster-cell';

interface RosterTableProps {
  days: string[];
  staff: StaffSchedule[];
  /** Today in clinic time, to mark its column. */
  today: string;
}

/** The week as a grid: one row per person, one column per day. Scrolls sideways on a narrow screen. */
export function RosterTable({ days, staff, today }: RosterTableProps) {
  const text = schedulePage;

  if (staff.length === 0) {
    return (
      <Card>
        <p className="px-5 py-6 text-sm text-ink-500">{text.empty}</p>
      </Card>
    );
  }

  return (
    <Card className="overflow-x-auto">
      <table className="w-full min-w-[46rem] text-left text-sm">
        <thead className="border-b border-ink-100 bg-ink-50/60">
          <tr>
            <th scope="col" className="px-3 py-2 font-medium text-ink-600">
              {text.person}
            </th>
            {days.map((day) => {
              const { weekday, date } = dayChipParts(day);

              return (
                <th
                  key={day}
                  scope="col"
                  className={`border-l border-ink-100 px-2 py-2 font-medium ${day === today ? 'text-brand-700' : 'text-ink-600'}`}
                >
                  {weekday} {date}
                </th>
              );
            })}
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {staff.map((person) => (
            <tr key={person.staff_id}>
              <th scope="row" className="px-3 py-2 align-top font-medium text-ink-900">
                {person.display_name}
              </th>
              {days.map((day) => (
                <RosterCell key={day} person={person} day={day} />
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </Card>
  );
}

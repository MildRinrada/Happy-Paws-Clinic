import { Badge } from '@/components/ui/badge';
import { Card, CardHeader } from '@/components/ui/card';
import { formatLongDate } from '@/lib/format/datetime';
import { hoursSettings } from '../../content/hours-settings';
import type { Holiday } from '../../types/clinic.types';
import { AddHolidayForm } from './add-holiday-form';

interface HolidaysCardProps {
  holidays: Holiday[];
  branches: { id: string; name: string }[];
  today: string;
}

/** The special closures still to come, and the form that declares another. */
export function HolidaysCard({ holidays, branches, today }: HolidaysCardProps) {
  const text = hoursSettings.holidays;
  const branchName = (id?: string) => (id ? (branches.find((branch) => branch.id === id)?.name ?? id) : text.wholeClinic);

  return (
    <div className="space-y-3">
      <Card>
        <CardHeader title={text.heading} />
        {holidays.length === 0 ? (
          <p className="px-5 py-6 text-sm text-ink-500">{text.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {holidays.map((holiday) => (
              <li key={holiday.id} className="flex flex-wrap items-center justify-between gap-2 px-5 py-3">
                <div>
                  <p className="font-medium text-ink-900">{holiday.name}</p>
                  <p className="text-sm text-ink-600">{formatLongDate(`${holiday.date}T00:00:00+07:00`)}</p>
                </div>
                <Badge tone={holiday.branch_id ? 'blue' : 'amber'}>{branchName(holiday.branch_id)}</Badge>
              </li>
            ))}
          </ul>
        )}
        <p className="border-t border-ink-100 px-5 py-3 text-xs text-ink-500">{text.note}</p>
      </Card>

      <AddHolidayForm branches={branches} today={today} />
    </div>
  );
}

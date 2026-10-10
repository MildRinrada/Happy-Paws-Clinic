import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { formatLongDate } from '@/lib/format/datetime';
import { demandForecastPage } from '../../content/forecast-page';
import type { DemandDay } from '../../types/forecast.types';
import { DemandHourRow } from './demand-hour-row';

/** One day of the forecast: its totals in the heading, then hour by hour. */
export function DemandDayCard({ day }: { day: DemandDay }) {
  const text = demandForecastPage;

  return (
    <Card>
      <CardHeader
        title={formatLongDate(`${day.date}T00:00:00Z`)}
        aside={day.high_load_hours > 0 ? <Badge tone="red">{text.busyHours(day.high_load_hours)}</Badge> : undefined}
      />
      <CardBody className="space-y-2 px-0 py-2">
        <p className="px-3 text-sm text-ink-600">{text.dayTotals(day.booked_appointments, day.expected_walk_ins)}</p>
        {day.hours.length === 0 ? (
          <p className="px-3 text-sm text-ink-500">{text.noHours}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {day.hours.map((hour) => (
              <DemandHourRow key={hour.hour} hour={hour} />
            ))}
          </ul>
        )}
      </CardBody>
    </Card>
  );
}

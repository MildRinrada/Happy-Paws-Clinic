import Link from 'next/link';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { upcomingVisits } from '../../content/upcoming-visits';
import type { UpcomingView } from '../../lib/load-upcoming';
import { UPCOMING_HOURS } from '../../lib/today-settings';
import { UpcomingVisitRow } from './upcoming-visit-row';

/** "นัดถัดไป 2 ชม.": who is due at the counter soon, beside the to-do box. */
export function UpcomingVisits({ view, calendarHref }: { view: UpcomingView; calendarHref: string }) {
  const text = upcomingVisits;

  return (
    <Card className="h-full">
      <CardHeader
        title={text.heading(UPCOMING_HOURS)}
        aside={
          <Link href={calendarHref} className="text-sm text-brand-700 hover:underline">
            {text.calendar}
          </Link>
        }
      />
      <CardBody className="px-2 py-2">
        {view.visits.length === 0 ? (
          <p className="px-3 py-3 text-sm text-ink-600">{text.empty(UPCOMING_HOURS)}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {view.visits.map((visit) => (
              <UpcomingVisitRow key={visit.id} visit={visit} href={calendarHref} />
            ))}
          </ul>
        )}
        {view.more > 0 && <p className="px-3 pt-1 pb-1 text-xs text-ink-500">{text.more(view.more)}</p>}
      </CardBody>
    </Card>
  );
}

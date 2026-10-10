import { vaccinePage } from '../content/vaccine-schedule-page';
import type { ScheduleActions as Actions, Species } from '../types/vaccine-schedule.types';
import { BookVaccineCard } from './book-vaccine-card';
import { LineReminderCard } from './line-reminder-card';
import { PrintScheduleCard } from './print-schedule-card';

/** What to do with the timeline: be reminded on LINE, book the vaccination, print it. */
export function ScheduleActions({ actions, species }: { actions: Actions; species: Species }) {
  return (
    <section aria-labelledby="vaccine-actions-heading" className="print:hidden">
      <h2 id="vaccine-actions-heading" className="text-xl font-semibold">
        {vaccinePage.actions.heading}
      </h2>
      <ul className="mt-4 grid gap-4 lg:grid-cols-2">
        {actions.line && <LineReminderCard line={actions.line} />}
        <BookVaccineCard href={actions.book[species]} />
        <PrintScheduleCard />
      </ul>
    </section>
  );
}

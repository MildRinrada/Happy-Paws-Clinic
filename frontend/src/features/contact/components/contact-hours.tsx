import { OpeningHoursTable } from '@/features/about/components/opening-hours-table';
import { clinicToday } from '@/features/about/lib/load-visit-info';
import { inWeekOrder } from '@/features/clinic/lib/weekdays';
import type { OpeningHours } from '@/features/clinic/types/clinic.types';
import { contactPage } from '../content/contact-page';

/** The week's hours, today marked: the same table the about page shows, from the same settings. */
export function ContactHours({ hours }: { hours: OpeningHours | null }) {
  return (
    <section aria-labelledby="contact-hours-heading" className="space-y-3">
      <h2 id="contact-hours-heading" className="text-xl font-semibold">
        {contactPage.hoursHeading}
      </h2>
      {hours ? (
        <OpeningHoursTable days={inWeekOrder(hours.days)} today={clinicToday()} />
      ) : (
        <p className="text-[15px] text-site-muted">{contactPage.hoursUnavailable}</p>
      )}
    </section>
  );
}

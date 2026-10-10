import type { Metadata } from 'next';
import { BookingWizard } from '@/features/booking/components/booking-wizard';
import { bookingPage } from '@/features/booking/content/booking-page';
import { loadBookingOptions } from '@/features/booking/lib/load-booking-options';
import { preselectTime } from '@/features/booking/lib/preselect-time';
import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { PageHeading } from '@/features/portal/components/shared/page-heading';

export const metadata: Metadata = { title: `${bookingPage.title} · Happy Paws Clinic` };

/**
 * Booking an appointment online, for one of the owner's own pets. `?pet=` preselects a pet, and
 * `?service=` and `?branch=` a service (a signed-in owner arrives from `/book` that way). `?date=`
 * with `?time=` keeps a time on top of them (the home page's "จองเวลานี้", through `/book`), checked
 * against the day's free times by the same `preselectTime` as `/book`; the pet is still asked first.
 */
export default async function PortalBookPage({
  searchParams,
}: {
  searchParams: Promise<{ pet?: string; service?: string; branch?: string; date?: string; time?: string }>;
}) {
  const { pet, service, branch, date, time } = await searchParams;
  const { options, initial } = await loadBookingOptions(pet, service, branch);
  const start = options.pets.length > 0 ? await preselectTime(initial, date, time) : undefined;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <PageHeading title={bookingPage.title} intro={bookingPage.intro} />

      <div className="rounded-2xl bg-white p-5 shadow-sm sm:p-6">
        {options.pets.length === 0 ? (
          <EmptyLine>{bookingPage.details.noPets}</EmptyLine>
        ) : (
          <BookingWizard options={options} initial={initial} start={start} />
        )}
      </div>
    </div>
  );
}

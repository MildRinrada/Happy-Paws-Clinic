import type { Metadata } from 'next';
import { BookingWizard } from '@/features/booking/components/booking-wizard';
import { bookingPage } from '@/features/booking/content/booking-page';
import { loadBookingOptions } from '@/features/booking/lib/load-booking-options';
import { preselectVet } from '@/features/booking/lib/preselect-booking';
import { preselectTime } from '@/features/booking/lib/preselect-time';
import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { PageHeading } from '@/features/portal/components/shared/page-heading';

export const metadata: Metadata = { title: `${bookingPage.title} · Happy Paws Clinic` };

/**
 * Booking an appointment online, for one of the owner's own pets. `?pet=` preselects a pet, and
 * `?service=` and `?branch=` a service (a signed-in owner arrives from `/book` that way). `?date=`
 * with `?time=` keeps a time on top of them (the home page's "จองเวลานี้", through `/book`), checked
 * against the day's free times by the same `preselectTime` as `/book`; the pet is still asked first.
 * `?vet=` starts the times on that vet's (the team page's "จองนัดกับหมอท่านนี้", through `/book`),
 * checked by the same `preselectVet`.
 */
export default async function PortalBookPage({
  searchParams,
}: {
  searchParams: Promise<{ pet?: string; service?: string; branch?: string; date?: string; time?: string; vet?: string }>;
}) {
  const { pet, service, branch, date, time, vet } = await searchParams;
  const { options, initial } = await loadBookingOptions(pet, service, branch);
  const vetId = preselectVet(options, vet);
  const start = options.pets.length > 0 ? await preselectTime(initial, date, time, vetId) : undefined;

  return (
    <div className="space-y-8">
      <PageHeading title={bookingPage.title} intro={bookingPage.intro} />

      {options.pets.length === 0 ? (
        <div className="max-w-2xl rounded-[18px] bg-white p-5 sm:p-6">
          <EmptyLine>{bookingPage.details.noPets}</EmptyLine>
        </div>
      ) : (
        <BookingWizard options={options} initial={initial} start={start} vet={vetId} />
      )}
    </div>
  );
}

import type { Metadata } from 'next';
import { BookingWizard } from '@/features/booking/components/booking-wizard';
import { bookingPage } from '@/features/booking/content/booking-page';
import { loadBookingOptions } from '@/features/booking/lib/load-booking-options';
import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { PageHeading } from '@/features/portal/components/shared/page-heading';

export const metadata: Metadata = { title: `${bookingPage.title} · Happy Paws Clinic` };

/**
 * Booking an appointment online, for one of the owner's own pets. `?pet=` preselects a pet, and
 * `?service=` and `?branch=` a service (a signed-in owner arrives from `/book` that way).
 */
export default async function PortalBookPage({
  searchParams,
}: {
  searchParams: Promise<{ pet?: string; service?: string; branch?: string }>;
}) {
  const { pet, service, branch } = await searchParams;
  const { options, initial } = await loadBookingOptions(pet, service, branch);

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <PageHeading title={bookingPage.title} intro={bookingPage.intro} />

      <div className="rounded-2xl bg-white p-5 shadow-sm sm:p-6">
        {options.pets.length === 0 ? (
          <EmptyLine>{bookingPage.details.noPets}</EmptyLine>
        ) : (
          <BookingWizard options={options} initial={initial} />
        )}
      </div>
    </div>
  );
}

import type { Metadata } from 'next';
import { headers } from 'next/headers';
import { SiteContainer } from '@/components/layout/site-container';
import { redirectIfSignedIn } from '@/features/authentication/lib/redirect-signed-in';
import { GuestBookingWizard } from '@/features/booking/components/guest/guest-booking-wizard';
import { SignInInstead } from '@/features/booking/components/guest/sign-in-instead';
import { guestBookingPage } from '@/features/booking/content/guest-booking-page';
import { loadGuestBookingOptions } from '@/features/booking/lib/load-guest-booking-options';

export const metadata: Metadata = {
  title: `${guestBookingPage.metaTitle} | Happy Paws Clinic`,
  description: guestBookingPage.metaDescription,
};

// Whether the visitor is already signed in is read from their cookies, so this is never prerendered.
export const dynamic = 'force-dynamic';

/**
 * Booking without signing up first (docs/contract-extensions.md § 27). Anyone can pick a service,
 * a branch, a day and a time; the phone and its code come last, and sign the visitor in.
 *
 * `?service=` and `?branch=` preselect the form (the services page links here that way). Someone
 * already signed in as a customer gets the portal's own booking page with the same choice, and a
 * member of staff goes to the console, as from `/login`.
 */
export default async function BookPage({ searchParams }: { searchParams: Promise<{ service?: string; branch?: string }> }) {
  const { service, branch } = await searchParams;
  const query = new URLSearchParams();
  if (service) query.set('service', service);
  if (branch) query.set('branch', branch);
  const portalBooking = query.size > 0 ? `/portal/book?${query}` : '/portal/book';

  // Not while one of this page's own actions is running. Proving the number signs the visitor in
  // and sets the session cookies, which re-renders this page inside that action; sending them away
  // then would drop the form one step before the end.
  if (!(await headers()).has('next-action')) {
    await redirectIfSignedIn(portalBooking);
  }

  const { options, initial } = await loadGuestBookingOptions(service, branch);

  return (
    <main className="bg-site-soft py-10">
      <SiteContainer>
        <div className="mx-auto max-w-2xl space-y-6">
          <div className="space-y-2">
            <h1 className="text-2xl font-semibold text-ink-900">{guestBookingPage.title}</h1>
            <p className="text-[15px] text-site-muted">{guestBookingPage.intro}</p>
            <SignInInstead next={portalBooking} />
          </div>

          <div className="rounded-2xl bg-white p-5 shadow-sm sm:p-6">
            <GuestBookingWizard options={options} initial={initial} />
          </div>
        </div>
      </SiteContainer>
    </main>
  );
}

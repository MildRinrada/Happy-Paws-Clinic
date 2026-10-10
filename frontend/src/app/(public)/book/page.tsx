import type { Metadata } from 'next';
import { headers } from 'next/headers';
import { SiteContainer } from '@/components/layout/site-container';
import { redirectIfSignedIn } from '@/features/authentication/lib/redirect-signed-in';
import { GuestBookingWizard } from '@/features/booking/components/guest/guest-booking-wizard';
import { SignInInstead } from '@/features/booking/components/guest/sign-in-instead';
import { guestBookingPage } from '@/features/booking/content/guest-booking-page';
import { loadGuestBookingOptions } from '@/features/booking/lib/load-guest-booking-options';
import { preselectTime } from '@/features/booking/lib/preselect-time';
import { validReferralCode } from '@/features/referrals/lib/valid-referral-code';

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
 * `?service=` and `?branch=` preselect the form (the services page links here that way), and
 * `?date=` with `?time=` a time on top of them (the home page's "ว่างเร็วสุด" button), checked
 * against the day's free times before the form draws. Someone already signed in as a customer gets
 * the portal's own booking page with the same service, branch, day and time, and a member of staff
 * goes to the console, as from `/login`.
 *
 * `?ref=` is a friend's referral code (docs/contract-extensions.md § 39), from `/r/{code}`. It is
 * checked again here, since the address can be typed; a code that can be used is sent with the
 * phone's proof and shows a one-line note, anything else is dropped without a word. It is not
 * carried to the portal: someone signed in is a customer already.
 */
export default async function BookPage({
  searchParams,
}: {
  searchParams: Promise<{ service?: string; branch?: string; date?: string; time?: string; ref?: string }>;
}) {
  const { service, branch, date, time, ref } = await searchParams;
  const query = new URLSearchParams();
  if (service) query.set('service', service);
  if (branch) query.set('branch', branch);
  // The time goes along too, so a signed-in owner sent to the portal keeps the one they tapped.
  if (date && time) {
    query.set('date', date);
    query.set('time', time);
  }
  const portalBooking = query.size > 0 ? `/portal/book?${query}` : '/portal/book';

  // Not while one of this page's own actions is running. Proving the number signs the visitor in
  // and sets the session cookies, which re-renders this page inside that action; sending them away
  // then would drop the form one step before the end.
  if (!(await headers()).has('next-action')) {
    await redirectIfSignedIn(portalBooking);
  }

  const [{ options, initial }, referralCode] = await Promise.all([
    loadGuestBookingOptions(service, branch),
    validReferralCode(ref),
  ]);
  const start = await preselectTime(initial, date, time);

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
            <GuestBookingWizard options={options} initial={initial} start={start} referralCode={referralCode} />
          </div>
        </div>
      </SiteContainer>
    </main>
  );
}

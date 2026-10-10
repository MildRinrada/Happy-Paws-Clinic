import Link from 'next/link';
import { guestBookingPage } from '../../content/guest-booking-page';

/** For an owner who already has an account: sign in, then book on the portal with the same choice. */
export function SignInInstead({ next }: { next: string }) {
  return (
    <p className="text-sm text-site-muted">
      {guestBookingPage.signInPrompt}{' '}
      <Link href={`/login?${new URLSearchParams({ next })}`} className="font-semibold text-site-teal underline">
        {guestBookingPage.signInLink}
      </Link>
    </p>
  );
}

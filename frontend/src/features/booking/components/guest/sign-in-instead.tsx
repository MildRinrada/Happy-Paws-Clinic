import Link from 'next/link';
import { guestBookingPage } from '../../content/guest-booking-page';

/** For an owner who already has an account: sign in, then book on the portal with the same choice. */
export function SignInInstead({ next }: { next: string }) {
  return (
    <p className="shrink-0 text-[15px] text-ink-700">
      {guestBookingPage.signInPrompt}{' '}
      <Link
        href={`/login?${new URLSearchParams({ next })}`}
        className="font-bold text-site-forest underline underline-offset-2 hover:text-site-teal"
      >
        {guestBookingPage.signInLink}
      </Link>
    </p>
  );
}

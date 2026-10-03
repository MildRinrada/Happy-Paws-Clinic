'use client';

import Link from 'next/link';
import { accountLink, signInLink, signUpLink } from '@/config/site-navigation';
import { useSignedInHint } from '@/lib/auth/use-signed-in-hint';

/** The account links at the bottom of the phone menu: sign in and sign up, or the account. */
export function MobileAccountLinks({ itemClassName }: { itemClassName: string }) {
  const signedIn = useSignedInHint();
  const links = signedIn ? [accountLink] : [signInLink, signUpLink];

  return (
    <>
      {links.map((link) => (
        <li key={link.label} className="sm:hidden">
          <Link href={link.href} className={`${itemClassName} font-semibold`}>
            {link.label}
          </Link>
        </li>
      ))}
    </>
  );
}

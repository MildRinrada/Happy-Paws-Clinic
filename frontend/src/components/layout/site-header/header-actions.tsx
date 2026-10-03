'use client';

import Link from 'next/link';
import { CtaLink } from '@/components/ui/cta-link';
import { accountLink, signInLink, signUpLink } from '@/config/site-navigation';
import { useSignedInHint } from '@/lib/auth/use-signed-in-hint';

/**
 * Sign in and sign up at the header's right edge, or "บัญชีของฉัน" for someone already signed in.
 *
 * Hidden on a phone, where the logo and the menu button are all the header has room for; the
 * same links are in the menu there.
 */
export function HeaderActions() {
  const signedIn = useSignedInHint();

  if (signedIn) {
    return (
      <div className="hidden items-center sm:flex">
        <CtaLink href={accountLink.href}>{accountLink.label}</CtaLink>
      </div>
    );
  }

  return (
    <div className="hidden items-center gap-6 sm:flex">
      <Link href={signInLink.href} className="text-sm font-bold text-ink-800 hover:text-site-teal">
        {signInLink.label}
      </Link>
      <CtaLink href={signUpLink.href}>{signUpLink.label}</CtaLink>
    </div>
  );
}

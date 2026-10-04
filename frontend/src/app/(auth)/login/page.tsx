import type { Metadata } from 'next';
import { MobileBrand } from '@/features/authentication/components/sign-in/mobile-brand';
import { SignInForm } from '@/features/authentication/components/sign-in/sign-in-form';
import { SignInHeading } from '@/features/authentication/components/sign-in/sign-in-heading';
import { SignInNotice } from '@/features/authentication/components/sign-in/sign-in-notice';
import { SignInShowcase } from '@/features/authentication/components/sign-in/sign-in-showcase';
import { signInPage } from '@/features/authentication/content/sign-in-page';
import { redirectIfSignedIn } from '@/features/authentication/lib/redirect-signed-in';

export const metadata: Metadata = { title: `${signInPage.metaTitle} · Happy Paws Clinic` };

// Whether the visitor is already signed in is read from their cookies, so this is never prerendered.
export const dynamic = 'force-dynamic';

/**
 * Sign-in for customers and staff alike: the photo slideshow on the left, the form on the right.
 * Someone already signed in is sent on to where their account belongs.
 *
 * `?next=` is the page the visitor was sent here from (the middleware adds it). It is passed along
 * as it came and checked on the server before anyone is sent there.
 *
 * `?notice=` picks one of a few fixed messages, e.g. after signing up when the account was made
 * but not signed in (see `SignInNotice`).
 */
export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string; notice?: string }>;
}) {
  const { next, notice } = await searchParams;
  await redirectIfSignedIn(next);

  return (
    <main className="grid min-h-dvh bg-white lg:grid-cols-2">
      <SignInShowcase />

      <section className="flex items-center justify-center px-4 py-12 sm:px-8">
        <div className="w-full max-w-[380px]">
          <MobileBrand />
          <SignInHeading />
          <SignInNotice notice={notice} />
          <SignInForm next={next} />
        </div>
      </section>
    </main>
  );
}

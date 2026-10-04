import type { Metadata } from 'next';
import { RegisterFlow } from '@/features/authentication/components/register/register-flow';
import { AuthHeading } from '@/features/authentication/components/shared/auth-heading';
import { AuthSplitLayout } from '@/features/authentication/components/shared/auth-split-layout';
import { registerPage } from '@/features/authentication/content/register-page';
import { redirectIfSignedIn } from '@/features/authentication/lib/redirect-signed-in';

export const metadata: Metadata = { title: `${registerPage.metaTitle} | Happy Paws Clinic` };

// Whether the visitor is already signed in is read from their cookies, so this is never prerendered.
export const dynamic = 'force-dynamic';

/**
 * Customer sign-up: a phone number proved with a code by SMS, then a name and a password. The new
 * customer is signed in and lands on the portal. Someone already signed in is sent on to where
 * their account belongs, as on `/login`.
 */
export default async function RegisterPage() {
  await redirectIfSignedIn();

  return (
    <AuthSplitLayout>
      <AuthHeading heading={registerPage.heading} subheading={registerPage.subheading} />
      <RegisterFlow />
    </AuthSplitLayout>
  );
}

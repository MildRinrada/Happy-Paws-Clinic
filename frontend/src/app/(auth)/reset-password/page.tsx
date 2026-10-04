import type { Metadata } from 'next';
import { ResetPasswordFlow } from '@/features/authentication/components/password-reset/reset-password-flow';
import { AuthHeading } from '@/features/authentication/components/shared/auth-heading';
import { AuthSplitLayout } from '@/features/authentication/components/shared/auth-split-layout';
import { BackToSignInLink } from '@/features/authentication/components/shared/back-to-sign-in-link';
import { resetPasswordPage } from '@/features/authentication/content/password-reset-page';

export const metadata: Metadata = {
  title: `${resetPasswordPage.metaTitle} | Happy Paws Clinic`,
  // Only ever opened from a personal link. Nothing here is for a search engine.
  robots: { index: false, follow: false },
};

/**
 * Where a reset link lands: `/reset-password#token={token}`, the shape the backend's
 * `Auth:SignIn:PasswordResetUrlTemplate` builds. The token is in the fragment, which only the
 * browser can read, so the page itself is static and the flow reads it on the client.
 */
export default function ResetPasswordPage() {
  return (
    <AuthSplitLayout>
      <AuthHeading heading={resetPasswordPage.heading} subheading={resetPasswordPage.subheading} />
      <ResetPasswordFlow />
      <BackToSignInLink />
    </AuthSplitLayout>
  );
}

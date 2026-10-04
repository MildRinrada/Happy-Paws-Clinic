import type { Metadata } from 'next';
import { ForgotPasswordPanel } from '@/features/authentication/components/password-reset/forgot-password-panel';
import { AuthHeading } from '@/features/authentication/components/shared/auth-heading';
import { AuthSplitLayout } from '@/features/authentication/components/shared/auth-split-layout';
import { BackToSignInLink } from '@/features/authentication/components/shared/back-to-sign-in-link';
import { forgotPasswordPage } from '@/features/authentication/content/password-reset-page';

export const metadata: Metadata = { title: `${forgotPasswordPage.metaTitle} | Happy Paws Clinic` };

/**
 * Asks for a password-reset link, for customers and staff alike (the backend serves both). The
 * link arrives by email when the account has one, otherwise by SMS, and opens `/reset-password`.
 */
export default function ForgotPasswordPage() {
  return (
    <AuthSplitLayout>
      <AuthHeading heading={forgotPasswordPage.heading} subheading={forgotPasswordPage.subheading} />
      <ForgotPasswordPanel />
      <BackToSignInLink />
    </AuthSplitLayout>
  );
}

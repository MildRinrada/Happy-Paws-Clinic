import type { Metadata } from 'next';
import { redirect } from 'next/navigation';
import { PageHeader } from '@/components/ui/page-header';
import { listSessions } from '@/features/account/api/sessions';
import { PasswordForm } from '@/features/account/components/password/password-form';
import { ProfileForm } from '@/features/account/components/profile/profile-form';
import { SessionsCard } from '@/features/account/components/sessions/sessions-card';
import { TwoFactorCard } from '@/features/account/components/two-factor/two-factor-card';
import { accountPage } from '@/features/account/content/account-page';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { SIGN_IN_PATH, authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: accountPage.metaTitle };

/**
 * The signed-in staff member's own account. No scope is checked: every account may read and change
 * itself (`/me` has no `[RequireScope]`), so whoever reached the console can use this page.
 */
export default async function AccountPage() {
  const context = await loadStaffContext();

  // The staff layout has already sent a non-staff session away; this only narrows the type.
  if (!context) {
    redirect(SIGN_IN_PATH);
  }

  const sessions = await authed(listSessions);
  const { me } = context;

  return (
    <div className="max-w-3xl space-y-6">
      <PageHeader title={accountPage.title} subtitle={accountPage.subtitle} />
      <ProfileForm me={me} />
      <PasswordForm />
      <TwoFactorCard twoFactor={me.two_factor} />
      <SessionsCard sessions={sessions} />
    </div>
  );
}

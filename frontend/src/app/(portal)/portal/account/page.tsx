import type { Metadata } from 'next';
import { getMe } from '@/features/account/api/getMe';
import { listSessions } from '@/features/account/api/sessions';
import { getConsents } from '@/features/portal/api/privacy';
import { AccountSection } from '@/features/portal/components/account/account-section';
import { ConsentsForm } from '@/features/portal/components/account/consents-form';
import { ContactSettings } from '@/features/portal/components/account/contact-settings';
import { DataRequestForm } from '@/features/portal/components/account/data-request-form';
import { PasswordForm } from '@/features/portal/components/account/password-form';
import { SessionList } from '@/features/portal/components/account/session-list';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { portalAccount } from '@/features/portal/content/portal-account';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: `${portalAccount.title} · Happy Paws Clinic` };

/** The owner's own account: how to reach them, how they sign in, and what they agree to. */
export default async function PortalAccountPage() {
  const [me, consents, sessions] = await authed(() => Promise.all([getMe(), getConsents(), listSessions()]));
  const text = portalAccount;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <PageHeading title={text.title} intro={text.intro} />

      <ContactSettings me={me} />

      <AccountSection title={text.password.title}>
        <PasswordForm />
      </AccountSection>

      <AccountSection title={text.consents.title} intro={text.consents.intro}>
        <ConsentsForm consents={consents.consents} />
      </AccountSection>

      <AccountSection title={text.sessions.title}>
        <SessionList sessions={sessions} />
      </AccountSection>

      <AccountSection title={text.dataRequest.title} intro={text.dataRequest.intro}>
        <DataRequestForm />
      </AccountSection>
    </div>
  );
}

import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { invitationPage } from '../../content/invitation-page';

// Not `SIGN_IN_PATH` from lib/auth/session: that module reads cookies and is server-only, and this
// renders inside the client-side flow.
const SIGN_IN_HREF = '/login';

/** The account exists. The new member signs in like everyone else; accepting gave no session. */
export function InvitationAccepted({ email }: { email: string }) {
  const text = invitationPage.accepted;

  return (
    <div className="space-y-4 text-center">
      <h2 className="text-base font-semibold text-ink-900">{text.title}</h2>
      <p className="font-medium text-ink-900">{email}</p>
      <p className="text-sm text-ink-600">{text.body}</p>
      <Link href={SIGN_IN_HREF} className={buttonClass('primary', 'md', 'w-full')}>
        {text.signIn}
      </Link>
    </div>
  );
}

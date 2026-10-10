import { Alert } from '@/components/ui/alert';
import { invitationPage } from '../../content/invitation-page';
import { formatReviewDate } from '../../lib/review-dates';
import type { InvitationLoad } from '../../lib/load-invitation';
import { InvitationClosed } from './invitation-closed';
import { InvitationReviewForm } from './invitation-review-form';
import { InvitationVisit } from './invitation-visit';

/** The invitation page under its heading: the form when the link works, and why not when it does not. */
export function InvitationBody({ load, token }: { load: InvitationLoad; token: string }) {
  const text = invitationPage;

  if (load.kind === 'unavailable') {
    return <Alert tone="info">{text.unavailable}</Alert>;
  }

  if (load.kind === 'missing') {
    return <InvitationClosed title={text.notFoundTitle} body={text.notFoundBody} />;
  }

  const { invitation } = load;

  if (invitation.status === 'used') {
    return <InvitationClosed title={text.usedTitle} body={text.usedBody} />;
  }

  if (invitation.status === 'expired') {
    return <InvitationClosed title={text.expiredTitle} body={text.expiredBody} />;
  }

  return (
    <div className="space-y-6">
      <InvitationVisit invitation={invitation} />
      <InvitationReviewForm token={token} />
      <p className="text-xs text-site-muted">{text.expiresOn(formatReviewDate(invitation.expires_at))}</p>
    </div>
  );
}

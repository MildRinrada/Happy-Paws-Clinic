import { invitationPage } from '../../content/invitation-page';
import { formatVisitDay } from '../../lib/review-dates';
import type { ReviewInvitationView } from '../../types/review.types';

/** Which visit the link is for: the day, the service and the pet, and nothing else about anyone. */
export function InvitationVisit({ invitation }: { invitation: ReviewInvitationView }) {
  const text = invitationPage;
  const { visit } = invitation;

  return (
    <div className="space-y-2 rounded-2xl bg-site-soft px-5 py-4 text-sm text-ink-800">
      <p className="text-base font-semibold text-ink-900">{text.visitOn(formatVisitDay(visit.visit_on))}</p>
      <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
        {visit.pet_name && (
          <>
            <dt className="text-site-muted">{text.pet}</dt>
            <dd className="min-w-0 break-words">{visit.pet_name}</dd>
          </>
        )}
        {visit.service_name && (
          <>
            <dt className="text-site-muted">{text.service}</dt>
            <dd className="min-w-0 break-words">{visit.service_name}</dd>
          </>
        )}
      </dl>
    </div>
  );
}

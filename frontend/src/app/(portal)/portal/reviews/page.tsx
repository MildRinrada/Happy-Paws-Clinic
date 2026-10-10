import type { Metadata } from 'next';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { listPendingReviews } from '@/features/reviews/api/portalReviews';
import { PendingVisitList } from '@/features/reviews/components/portal/pending-visit-list';
import { portalReviews } from '@/features/reviews/content/portal-reviews';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: portalReviews.metaTitle };

/**
 * The owner's visits of the last 60 days that have no review yet (docs/contract-extensions.md
 * § 36), each with the review form. A review waits for the clinic to read it before it shows on the
 * site, and is not edited afterwards. This is the page a review message links to for an owner with
 * a portal account.
 */
export default async function PortalReviewsPage() {
  const pending = await authed(listPendingReviews);

  return (
    <div className="space-y-6">
      <PageHeading title={portalReviews.title} intro={portalReviews.intro} />
      <PendingVisitList pending={pending} />
    </div>
  );
}

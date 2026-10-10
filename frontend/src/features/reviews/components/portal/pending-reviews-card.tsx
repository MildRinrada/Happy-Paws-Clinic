import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { SectionCard } from '@/features/portal/components/shared/section-card';
import { portalReviews } from '../../content/portal-reviews';
import { formatVisitDay } from '../../lib/review-dates';
import { loadPendingReviews } from '../../lib/load-pending-reviews';
import { reviewPaths } from '../../lib/review-paths';

/**
 * "รีวิวการมาครั้งล่าสุด" on the portal home: the newest visit still waiting for a review, and a
 * link to the review page. Nothing at all when there is none, or when the list could not be read.
 */
export async function PendingReviewsCard() {
  const pending = await loadPendingReviews();
  const latest = pending?.data[0];

  if (!pending || !latest) {
    return null;
  }

  const text = portalReviews.card;
  const more = text.more(pending.data.length);

  return (
    <SectionCard title={text.title}>
      <p className="break-words text-sm text-ink-800">{text.body(latest.pet_name, formatVisitDay(latest.visit_on))}</p>
      <div className="mt-4 flex flex-wrap items-center gap-3">
        <Link href={reviewPaths.portal} className={buttonClass('primary', 'sm')}>
          {text.action}
        </Link>
        {more && <span className="text-xs text-site-muted">{more}</span>}
      </div>
    </SectionCard>
  );
}

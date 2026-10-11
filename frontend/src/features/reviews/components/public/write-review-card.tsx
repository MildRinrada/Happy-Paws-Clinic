import { useId } from 'react';
import { SiteCard } from '@/components/site/site-card';
import { CtaLink } from '@/components/ui/cta-link';
import { writeReviewCard } from '../../content/reviews-page';
import { reviewPaths } from '../../lib/review-paths';

/**
 * "เคยพาน้องมาที่คลินิก?" and the button to the owner's review page in the portal. Someone not
 * signed in is sent to sign in first, and comes back to it after.
 */
export function WriteReviewCard() {
  const headingId = useId();

  return (
    <SiteCard as="section" tone="whiteFaint" radius="xl" padding="md" ariaLabelledBy={headingId}>
      <h2 id={headingId} className="text-base font-bold leading-snug text-site-forest">
        {writeReviewCard.heading}
      </h2>
      <p className="mt-2.5 text-sm leading-[22px] text-ink-700">{writeReviewCard.body}</p>
      <CtaLink href={reviewPaths.portal} variant="forest" size="lg" className="mt-3.5 w-full">
        {writeReviewCard.action}
      </CtaLink>
    </SiteCard>
  );
}

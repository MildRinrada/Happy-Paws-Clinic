import { CtaLink } from '@/components/ui/cta-link';
import { reviewsPage } from '../../content/reviews-page';

/** "รีวิวก่อนหน้านี้", centred under the cards: the next page of older reviews. */
export function OlderReviewsLink({ href }: { href: string }) {
  return (
    <div className="flex justify-center">
      <CtaLink href={href} variant="outlineForest" size="lg">
        {reviewsPage.next}
      </CtaLink>
    </div>
  );
}

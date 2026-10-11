import Link from 'next/link';
import { SectionHeading } from '@/components/site/section-heading';
import { ReviewCardList } from '@/features/reviews/components/public/review-card-list';
import { reviewPaths } from '@/features/reviews/lib/review-paths';
import type { PublicReview } from '@/features/reviews/types/review.types';
import { testimonialsHeading, testimonialsMore } from '../../content/testimonials';

/**
 * The reviews column: a heading, the newest published reviews in the owners' own words, and a link
 * to all of them. Only real, published reviews reach here (docs/contract-extensions.md § 36).
 */
export function TestimonialList({ reviews }: { reviews: PublicReview[] }) {
  return (
    <section id="reviews" aria-labelledby="reviews-heading" className="scroll-mt-6">
      <SectionHeading id="reviews-heading">{testimonialsHeading}</SectionHeading>
      <ReviewCardList reviews={reviews} />
      <Link href={reviewPaths.list} className="mt-5 inline-block text-[15px] font-medium text-site-teal hover:underline">
        {testimonialsMore}
      </Link>
    </section>
  );
}

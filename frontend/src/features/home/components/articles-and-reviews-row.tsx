import { SiteContainer } from '@/components/layout/site-container';
import { loadPublicReviews } from '@/features/reviews/lib/load-public-reviews';
import { homeReviewCount } from '../content/testimonials';
import { ArticleList } from './articles/article-list';
import { TestimonialList } from './testimonials/testimonial-list';

/**
 * Articles and reviews share a row: side by side on a wide screen, stacked on a narrow one.
 * The reviews are the newest published ones (`GET /reviews`, reused for five minutes). With none
 * published, or the API away, the reviews column is left out and the articles take the row: the
 * page never shows sample quotes as if a customer had written them.
 */
export async function ArticlesAndReviewsRow() {
  const reviews = (await loadPublicReviews(homeReviewCount))?.data ?? [];
  const withReviews = reviews.length > 0;

  return (
    <div className="pt-10">
      <SiteContainer className={withReviews ? 'grid gap-10 lg:grid-cols-2 lg:gap-9' : 'grid gap-10'}>
        <ArticleList />
        {withReviews && <TestimonialList reviews={reviews} />}
      </SiteContainer>
    </div>
  );
}

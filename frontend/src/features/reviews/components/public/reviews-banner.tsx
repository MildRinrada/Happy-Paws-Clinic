import { PageBanner } from '@/components/site/page-banner';
import { reviewsPage } from '../../content/reviews-page';

/** The dark green band at the top: what the reviews are and when the clinic takes one down. */
export function ReviewsBanner() {
  return <PageBanner size="wide" eyebrow={reviewsPage.eyebrow} heading={reviewsPage.title} lead={reviewsPage.intro} />;
}

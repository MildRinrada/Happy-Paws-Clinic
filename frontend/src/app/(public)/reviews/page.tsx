import { SiteContainer } from '@/components/layout/site-container';
import { ReviewsBanner } from '@/features/reviews/components/public/reviews-banner';
import { ReviewsBoard } from '@/features/reviews/components/public/reviews-board';
import { reviewsPage } from '@/features/reviews/content/reviews-page';
import { loadReviewList } from '@/features/reviews/lib/load-review-list';
import { readReviewListQuery } from '@/features/reviews/lib/read-review-list-query';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';

export const metadata = pageMetadata({
  title: reviewsPage.metaTitle,
  description: reviewsPage.metaDescription,
  path: sitePages.reviews.path,
});

interface Props {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/**
 * Every published review, newest first, with the average over published reviews only
 * (docs/contract-extensions.md § 36): a dark green band, the average and the card to write a
 * review at the left, and the reviews at the right under chips that filter them (`?filter=`).
 * Everything is read through `loadReviewList`. No star rating markup for search engines: Google
 * does not show a business's own reviews of itself as stars, so the page does not claim them.
 */
export default async function ReviewsPage({ searchParams }: Props) {
  const query = readReviewListQuery(await searchParams);
  const list = await loadReviewList(query);

  return (
    <main>
      <ReviewsBanner />
      <SiteContainer className="pb-14 pt-10">
        <ReviewsBoard list={list} query={query} />
      </SiteContainer>

      <BreadcrumbJsonLd trail={[sitePages.reviews]} />
    </main>
  );
}

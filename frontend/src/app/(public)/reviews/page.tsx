import { SiteContainer } from '@/components/layout/site-container';
import { Alert } from '@/components/ui/alert';
import { SectionHeading } from '@/features/home/components/shared/section-heading';
import { ReviewCardList } from '@/features/reviews/components/public/review-card-list';
import { ReviewPager } from '@/features/reviews/components/public/review-pager';
import { ReviewSummary } from '@/features/reviews/components/public/review-summary';
import { reviewsPage } from '@/features/reviews/content/reviews-page';
import { loadPublicReviews } from '@/features/reviews/lib/load-public-reviews';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';

export const metadata = pageMetadata({
  title: reviewsPage.metaTitle,
  description: reviewsPage.metaDescription,
  path: sitePages.reviews.path,
});

/** How many reviews a page shows. */
const PAGE_SIZE = 10;

/**
 * Every published review, newest first, with the average over published reviews only
 * (docs/contract-extensions.md § 36). No star rating markup for search engines: Google does not
 * show a business's own reviews of itself as stars, so the page does not claim them.
 */
export default async function ReviewsPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const params = await searchParams;
  const cursor = typeof params.cursor === 'string' && params.cursor ? params.cursor : undefined;
  const page = await loadPublicReviews(PAGE_SIZE, cursor);
  const text = reviewsPage;

  return (
    <main className="py-12">
      <SiteContainer>
        <div className="max-w-[720px] space-y-6">
          <div className="space-y-3">
            <SectionHeading id="reviews-heading">{text.title}</SectionHeading>
            <p className="text-sm leading-relaxed text-site-muted">{text.intro}</p>
          </div>

          {!page ? (
            <Alert tone="info">{text.unavailable}</Alert>
          ) : (
            <>
              {!cursor && <ReviewSummary summary={page.summary} />}
              {page.data.length === 0 ? (
                <p className="text-sm text-site-muted">{text.empty}</p>
              ) : (
                <ReviewCardList reviews={page.data} className="" />
              )}
              <ReviewPager nextCursor={page.page.next_cursor} paged={Boolean(cursor)} />
            </>
          )}
        </div>
      </SiteContainer>

      <BreadcrumbJsonLd trail={[sitePages.reviews]} />
    </main>
  );
}

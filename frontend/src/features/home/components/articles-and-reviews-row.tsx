import { ArticleList } from './articles/article-list';
import { SectionContainer } from './shared/section-container';
import { TestimonialList } from './testimonials/testimonial-list';

/** Articles and reviews share a row: side by side on a wide screen, stacked on a narrow one. */
export function ArticlesAndReviewsRow() {
  return (
    <div className="pt-10">
      <SectionContainer className="grid gap-10 lg:grid-cols-2 lg:gap-9">
        <ArticleList />
        <TestimonialList />
      </SectionContainer>
    </div>
  );
}

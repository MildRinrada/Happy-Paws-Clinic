import { PageBanner } from '@/components/site/page-banner';
import { articlesHeading } from '../../content/articles-page';
import { ArticleSearchForm } from './article-search-form';

/** The top of the page on white: eyebrow, heading and lead, with the search box at the right. */
export function ArticlesHeading({ q, tag }: { q?: string; tag?: string }) {
  return (
    <PageBanner
      size="flush"
      tone="white"
      eyebrow={articlesHeading.eyebrow}
      heading={articlesHeading.heading}
      lead={articlesHeading.lead}
      aside={<ArticleSearchForm q={q} tag={tag} />}
    />
  );
}

import { ArticleCardList } from '@/features/articles/components/article-card-list';
import { loadArticles } from '@/features/articles/lib/load-articles';
import { articlesHeading, homeArticleCount } from '../../content/articles';
import { SectionHeading } from '../shared/section-heading';

/**
 * The articles column: a heading and the newest articles under it, read from `GET /articles`.
 * What the articles say is data in the database, put there by the operator's `seed-articles`
 * command; only the heading and how many to show are set here.
 */
export async function ArticleList() {
  const articles = await loadArticles(homeArticleCount);

  return (
    <section id="articles" aria-labelledby="articles-heading" className="scroll-mt-6">
      <SectionHeading id="articles-heading">{articlesHeading}</SectionHeading>
      <ArticleCardList articles={articles} />
    </section>
  );
}

import type { ArticleSummary } from '../../types/article.types';
import { ArticleTile } from './article-tile';

/** The articles under the large card: three across on a wide screen, two on a tablet, one on a phone. */
export function ArticleGrid({ articles }: { articles: ArticleSummary[] }) {
  return (
    <ul className="grid gap-x-6 gap-y-10 md:grid-cols-2 lg:grid-cols-3">
      {articles.map((article) => (
        <ArticleTile key={article.id} article={article} />
      ))}
    </ul>
  );
}

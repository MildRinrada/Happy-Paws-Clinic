import { articleCards } from '../../content/articles-page';
import { formatArticleDate } from '../../lib/article-date';
import type { ArticleSummary } from '../../types/article.types';

interface Props {
  article: Pick<ArticleSummary, 'author_name' | 'published_at'>;
  /** Size, colour and margin. */
  className: string;
}

/** "โดย สพ.ญ. มินท์ | 29 กันยายน 2569": who wrote the article, and the day it went live. */
export function ArticleByline({ article, className }: Props) {
  return (
    <p className={className}>
      {articleCards.by} {article.author_name} | <time dateTime={article.published_at}>{formatArticleDate(article.published_at)}</time>
    </p>
  );
}

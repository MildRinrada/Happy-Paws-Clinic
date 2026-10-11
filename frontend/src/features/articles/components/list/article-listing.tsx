import { articleSearch, articleTopics, articlesList } from '../../content/articles-page';
import { articlesHref } from '../../lib/articles-href';
import type { ArticleList, ArticleListQuery } from '../../types/article-list.types';
import { ArticleGrid } from './article-grid';
import { ArticleListMessage } from './article-list-message';
import { ArticleSearchSummary } from './article-search-summary';
import { ArticleTopicNav } from './article-topic-nav';
import { FeaturedArticle } from './featured-article';
import { OlderArticlesLink } from './older-articles-link';

interface Props {
  /** `null` when the articles could not be read. */
  list: ArticleList | null;
  query: ArticleListQuery;
}

/**
 * Under the heading: the topic chips, then the newest article large and the rest in a grid, then
 * the button to older articles when there are more. A search, a topic or a later page shows the
 * grid alone, with no large card, since its first article is not the newest.
 */
export function ArticleListing({ list, query }: Props) {
  if (list === null) {
    return <ArticleListMessage title={articlesList.unavailable} />;
  }

  const { q, tag } = query;
  const narrowed = Boolean(q || tag || query.cursor);
  const featured = narrowed ? undefined : list.items[0];
  const rest = featured ? list.items.slice(1) : list.items;
  const empty = q
    ? { title: articleSearch.noMatch(q), hint: articleSearch.noMatchHint, showAll: true }
    : tag
      ? { title: articleTopics.empty(tag), showAll: true }
      : { title: articlesList.empty };

  return (
    <div className="space-y-8">
      <ArticleTopicNav tags={list.tags} current={tag} q={q} />
      {q && list.items.length > 0 && <ArticleSearchSummary q={q} count={list.items.length} />}

      {list.items.length === 0 && <ArticleListMessage {...empty} />}

      {featured && <FeaturedArticle article={featured} />}
      {rest.length > 0 && <ArticleGrid articles={rest} />}
      {list.nextCursor && <OlderArticlesLink href={articlesHref({ q, tag, cursor: list.nextCursor })} />}
    </div>
  );
}

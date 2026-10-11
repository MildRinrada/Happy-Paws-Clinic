import { listArticles } from '../api/listArticles';
import type { ArticleList, ArticleListQuery } from '../types/article-list.types';
import type { ArticleSummary } from '../types/article.types';
import { matchesArticleSearch } from './article-search';

/** The most the API gives in one read, which is every article the page can show for now. */
const LATEST_LIMIT = 50;

/**
 * One page of the public articles list for a search, a topic and a page, or `null` when the API
 * cannot be reached (the page then says so instead of failing).
 *
 * Every read of the list goes through here. The API has no search, topics or paging for the
 * public yet (`GET /articles?tag=&q=&cursor=` and `GET /articles/tags` await approval), so for now
 * this reads the newest 50 and searches their titles and summaries itself. It offers no topics and
 * no next page, and a cursor is ignored since there is nothing past the first page. When the API
 * can do these, only this body changes; the page already draws the chips and the next-page button
 * when `tags` and `nextCursor` come back.
 */
export async function loadArticleList({ q, tag }: ArticleListQuery): Promise<ArticleList | null> {
  let latest: ArticleSummary[];
  try {
    latest = await listArticles(LATEST_LIMIT);
  } catch {
    return null;
  }

  const items = latest.filter(
    (article) => (!tag || (article.tags ?? []).includes(tag)) && (!q || matchesArticleSearch(article, q)),
  );

  return { items, tags: [] };
}

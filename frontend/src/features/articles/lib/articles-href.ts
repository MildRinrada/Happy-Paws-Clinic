import type { ArticleListQuery } from '../types/article-list.types';

const ARTICLES_PATH = '/articles';

/** The articles page showing a search, a topic, or a later page, or all of them together. */
export function articlesHref({ q, tag, cursor }: ArticleListQuery = {}): string {
  const params = new URLSearchParams();

  if (tag) params.set('tag', tag);
  if (q) params.set('q', q);
  if (cursor) params.set('cursor', cursor);

  const search = params.toString();
  return search ? `${ARTICLES_PATH}?${search}` : ARTICLES_PATH;
}

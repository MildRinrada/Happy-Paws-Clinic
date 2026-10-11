import type { ArticleListQuery } from '../types/article-list.types';

/** The longest search kept. Anything longer is cut, not refused, so a pasted paragraph still searches. */
export const ARTICLE_SEARCH_MAX = 100;

function text(params: Record<string, string | string[] | undefined>, key: string): string | undefined {
  const raw = params[key];
  // Runs of spaces become one, so the search reads back the way the page matches it.
  const value = (Array.isArray(raw) ? raw[0] : raw)?.replace(/\s+/g, ' ').trim();
  return value ? value : undefined;
}

/** `?q=&tag=&cursor=` of the public articles page. Blank values are dropped, and a key given twice keeps its first. */
export function readArticleListQuery(params: Record<string, string | string[] | undefined>): ArticleListQuery {
  return {
    q: text(params, 'q')?.slice(0, ARTICLE_SEARCH_MAX),
    tag: text(params, 'tag'),
    cursor: text(params, 'cursor'),
  };
}

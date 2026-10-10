import type { ArticleStatus } from '../types/staff-article.types';

const STATUSES: ArticleStatus[] = ['draft', 'scheduled', 'published'];

function text(params: Record<string, string | string[] | undefined>, key: string): string | undefined {
  const value = params[key];
  return typeof value === 'string' && value.trim() ? value.trim() : undefined;
}

/** `?q=&status=&cursor=` of the staff article list. An unknown status is dropped rather than sent. */
export function readArticleQuery(params: Record<string, string | string[] | undefined>) {
  const status = text(params, 'status');

  return {
    q: text(params, 'q'),
    status: STATUSES.find((value) => value === status),
    cursor: text(params, 'cursor'),
  };
}

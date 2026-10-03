import { publicApi } from '@/lib/api/server-client';
import type { Article } from '../types/article.types';

/**
 * `GET /articles/{slug}` — one published article. Public. Answers 404 for a missing or a not yet
 * published article alike.
 *
 * Not in the API contract; it is a contract extension (docs/contract-extensions.md § 9).
 */
export function getArticle(slug: string): Promise<Article> {
  return publicApi().get<Article>(`/articles/${encodeURIComponent(slug)}`);
}

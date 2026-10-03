import { publicApi } from '@/lib/api/server-client';
import type { ArticleSummary } from '../types/article.types';

/**
 * `GET /articles?limit=` — published articles, newest first. Public.
 *
 * Not in the API contract; it is a contract extension (docs/contract-extensions.md § 9).
 */
export async function listArticles(limit?: number): Promise<ArticleSummary[]> {
  const { data } = await publicApi().get<{ data: ArticleSummary[] }>('/articles', {
    query: { limit },
  });

  return data;
}

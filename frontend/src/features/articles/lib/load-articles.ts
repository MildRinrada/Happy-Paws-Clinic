import { listArticles } from '../api/listArticles';
import type { ArticleSummary } from '../types/article.types';

/**
 * The newest articles for a public page, or `null` if the API cannot be reached.
 *
 * The public site must still render when the API is briefly away: a home page that errors because
 * its article column could not load is worse than one that says so in that column.
 */
export async function loadArticles(limit: number): Promise<ArticleSummary[] | null> {
  try {
    return await listArticles(limit);
  } catch {
    return null;
  }
}

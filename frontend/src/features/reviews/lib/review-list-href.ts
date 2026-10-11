import type { ReviewListQuery } from '../types/review-list.types';
import { reviewPaths } from './review-paths';

/** The public reviews page under a filter, a later page, or both. Every review, newest first, is plain `/reviews`. */
export function reviewListHref({ filter, cursor }: Partial<ReviewListQuery> = {}): string {
  const params = new URLSearchParams();

  if (filter && filter !== 'all') params.set('filter', filter);
  if (cursor) params.set('cursor', cursor);

  const search = params.toString();
  return search ? `${reviewPaths.list}?${search}` : reviewPaths.list;
}

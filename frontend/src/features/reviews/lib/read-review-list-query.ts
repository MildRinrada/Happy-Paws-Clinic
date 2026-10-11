import type { ReviewFilter, ReviewListQuery } from '../types/review-list.types';
import { REVIEW_FILTERS } from './review-filters';

type SearchParams = Record<string, string | string[] | undefined>;

function text(params: SearchParams, key: string): string | undefined {
  const raw = params[key];
  const value = (Array.isArray(raw) ? raw[0] : raw)?.trim();
  return value ? value : undefined;
}

/** `?filter=&cursor=` of the public reviews page. No filter, or an unknown one, is every review. */
export function readReviewListQuery(params: SearchParams): ReviewListQuery {
  const filter = text(params, 'filter');

  return {
    filter: REVIEW_FILTERS.find((value): value is ReviewFilter => value === filter) ?? 'all',
    cursor: text(params, 'cursor'),
  };
}

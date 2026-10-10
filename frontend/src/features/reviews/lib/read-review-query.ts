import type { ReviewStatus } from '../types/review.types';

type SearchParams = Record<string, string | string[] | undefined>;

const STATUSES: ReviewStatus[] = ['pending', 'published', 'rejected'];

function text(params: SearchParams, key: string): string | undefined {
  const value = params[key];
  return typeof value === 'string' && value.trim() ? value.trim() : undefined;
}

/** `?status=&cursor=` of the moderation page. No status, or an unknown one, is the pending tab. */
export function readReviewQuery(params: SearchParams): { status: ReviewStatus; cursor?: string } {
  const status = text(params, 'status');

  return {
    status: STATUSES.find((value) => value === status) ?? 'pending',
    cursor: text(params, 'cursor'),
  };
}

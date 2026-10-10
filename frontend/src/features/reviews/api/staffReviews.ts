import { staffApi } from '@/lib/api/server-client';
import type { RejectReason, ReviewStatus, StaffReview, StaffReviewPage } from '../types/review.types';

/**
 * Moderating reviews (docs/contract-extensions.md § 36). Every route needs `content:manage`. Staff
 * publish, reject for a listed reason, or answer; they never change a customer's words.
 */

const path = (id: string, action: string) => `/staff/reviews/${encodeURIComponent(id)}/${action}`;

/** `GET /staff/reviews?status=&cursor=`: newest written first. */
export function listStaffReviews(query: { status?: ReviewStatus; cursor?: string }): Promise<StaffReviewPage> {
  return staffApi().get<StaffReviewPage>('/staff/reviews', { query: { ...query, limit: 25 } });
}

/** `POST /staff/reviews/{id}/publish`: on the public site and in the average. */
export function publishReview(id: string, version: number): Promise<StaffReview> {
  return staffApi().post<StaffReview>(path(id, 'publish'), { ifMatch: version });
}

/** `POST /staff/reviews/{id}/reject`: off the site, for a listed reason. */
export function rejectReview(id: string, reason: RejectReason, version: number): Promise<StaffReview> {
  return staffApi().post<StaffReview>(path(id, 'reject'), { body: { reason }, ifMatch: version });
}

/** `POST /staff/reviews/{id}/reply`: the clinic's one public answer, written or rewritten. */
export function replyToReview(id: string, text: string, version: number): Promise<StaffReview> {
  return staffApi().post<StaffReview>(path(id, 'reply'), { body: { text }, ifMatch: version });
}

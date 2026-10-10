/**
 * Shapes of the review routes (docs/contract-extensions.md § 36). Field names are the API's
 * snake_case ones. Dates without a time (`visit_on`) are `YYYY-MM-DD` in clinic time.
 */

export type ReviewStatus = 'pending' | 'published' | 'rejected';

export type ReviewNameChoice = 'first_name' | 'anonymous';

export type RejectReason = 'personal_data' | 'offensive' | 'off_topic' | 'spam' | 'customer_request';

export type InvitationStatus = 'usable' | 'used' | 'expired';

/** The clinic's public answer under a review. */
export interface ReviewReply {
  text: string;
  replied_at: string;
}

/** One published review: a first name or "ลูกค้า", never a surname, phone or pet's name. */
export interface PublicReview {
  id: string;
  rating: number;
  text?: string | null;
  display_name: string;
  pet_species?: string | null;
  created_at: string;
  reply?: ReviewReply | null;
}

/** Over published reviews only. `by_rating` is keyed "1" to "5". */
export interface ReviewSummary {
  average?: number | null;
  count: number;
  by_rating: Record<string, number>;
}

/** `GET /reviews`. */
export interface PublicReviewPage {
  data: PublicReview[];
  page: { next_cursor?: string | null };
  summary: ReviewSummary;
}

/** A visit the signed-in owner can still review. */
export interface PendingVisit {
  visit_id: string;
  visit_on: string;
  branch_id: string;
  branch_name: string;
  service_name?: string | null;
  pet_id: string;
  pet_name: string;
  pet_species: string;
  reviewable_until: string;
}

/** `GET /portal/reviews/pending`. */
export interface PendingReviews {
  first_name?: string | null;
  data: PendingVisit[];
}

/** `GET /reviews/invitations/{token}`: the day, the service and the pet's name, nothing else. */
export interface ReviewInvitationView {
  status: InvitationStatus;
  expires_at: string;
  visit: { visit_on: string; service_name?: string | null; pet_name?: string | null };
}

/** What a customer gets back after writing a review. */
export interface SubmittedReview {
  id: string;
  status: ReviewStatus;
  rating: number;
  text?: string | null;
  display_name: string;
  created_at: string;
}

/** The score, the words and the name choice a form sends. */
export interface ReviewInput {
  rating: number;
  text?: string;
  display_name: ReviewNameChoice;
}

/** One review in the moderation list. */
export interface StaffReview {
  id: string;
  status: ReviewStatus;
  rating: number;
  text?: string | null;
  display_name: string;
  name_choice: ReviewNameChoice;
  pet_species?: string | null;
  source: 'portal' | 'invitation';
  created_at: string;
  visit: {
    visit_id: string;
    visit_on: string;
    branch_id: string;
    branch_name?: string | null;
    pet_id?: string | null;
    pet_name?: string | null;
  };
  customer: { id: string; name?: string | null };
  moderation: {
    moderated_at?: string | null;
    moderated_by_staff_id?: string | null;
    published_at?: string | null;
    reject_reason?: RejectReason | null;
    reject_reason_label?: string | null;
  };
  reply?: { text: string; replied_at: string; replied_by_staff_id?: string | null } | null;
  version: number;
}

/** `GET /staff/reviews`. */
export interface StaffReviewPage {
  data: StaffReview[];
  page: { next_cursor?: string | null };
}

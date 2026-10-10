'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { submitPortalReview } from '../api/portalReviews';
import { submitInvitationReview } from '../api/reviewInvitations';
import { REVIEW_TEXT_MAX } from '../content/review-form';
import { reviewPaths } from '../lib/review-paths';
import type { SubmittedReview } from '../types/review.types';

/**
 * Writing a review (docs/contract-extensions.md § 36), from the portal or through a one-time link.
 * The backend decides every rule: whose visit it is, the 60 days, one review per visit, the text.
 * These check the shape, make the call, and mark the pages that list pending visits stale.
 */

const review = z.object({
  rating: z.number().int().min(1).max(5),
  // Room for line breaks the backend cleans away; the backend counts what it keeps.
  text: z.string().max(REVIEW_TEXT_MAX * 2).optional(),
  display_name: z.enum(['first_name', 'anonymous']),
});

const portalInput = review.extend({ visit_id: z.string().trim().min(1).max(40) });

export async function submitPortalReviewAction(values: z.input<typeof portalInput>): Promise<ActionResult<SubmittedReview>> {
  const { visit_id: visitId, ...input } = portalInput.parse(values);
  const result = await runAction(() => submitPortalReview(visitId, { ...input, text: input.text?.trim() || undefined }));

  if (result.ok) {
    revalidatePath(reviewPaths.portal);
    revalidatePath('/portal');
    revalidatePath(reviewPaths.staff);
  }

  return result;
}

const invitationInput = review.extend({ token: z.string().trim().min(1).max(100) });

export async function submitInvitationReviewAction(
  values: z.input<typeof invitationInput>,
): Promise<ActionResult<SubmittedReview>> {
  const { token, ...input } = invitationInput.parse(values);
  const result = await runAction(() => submitInvitationReview(token, { ...input, text: input.text?.trim() || undefined }));

  if (result.ok) revalidatePath(reviewPaths.staff);
  return result;
}

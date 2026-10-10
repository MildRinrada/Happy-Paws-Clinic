'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { publishReview, rejectReview, replyToReview } from '../api/staffReviews';
import { reviewPaths } from '../lib/review-paths';
import type { StaffReview } from '../types/review.types';

/**
 * Publishing, rejecting and answering reviews (`content:manage`, docs/contract-extensions.md § 36).
 * The backend checks the scope, the state and the reason again. Every change marks the public pages
 * that show reviews stale, so a decision is on the site at once.
 */

const id = z.string().trim().min(1).max(40);
const version = z.number().int().nonnegative();

function refreshReviewPages() {
  revalidatePath('/');
  revalidatePath(reviewPaths.list);
  revalidatePath(reviewPaths.staff);
}

const step = z.object({ id, version });

export async function publishReviewAction(values: z.infer<typeof step>): Promise<ActionResult<StaffReview>> {
  const input = step.parse(values);
  const result = await runAction(() => publishReview(input.id, input.version));

  if (result.ok) refreshReviewPages();
  return result;
}

const reject = step.extend({ reason: z.enum(['personal_data', 'offensive', 'off_topic', 'spam', 'customer_request']) });

export async function rejectReviewAction(values: z.infer<typeof reject>): Promise<ActionResult<StaffReview>> {
  const input = reject.parse(values);
  const result = await runAction(() => rejectReview(input.id, input.reason, input.version));

  if (result.ok) refreshReviewPages();
  return result;
}

const reply = step.extend({ text: z.string().max(2_000) });

export async function replyToReviewAction(values: z.infer<typeof reply>): Promise<ActionResult<StaffReview>> {
  const input = reply.parse(values);
  const result = await runAction(() => replyToReview(input.id, input.text, input.version));

  if (result.ok) refreshReviewPages();
  return result;
}

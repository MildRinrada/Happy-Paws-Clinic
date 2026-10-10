'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import type { ActionResult } from '@/lib/actions/action-result';
import { reviewForm } from '../../content/review-form';
import type { ReviewInput, ReviewNameChoice, SubmittedReview } from '../../types/review.types';
import { NameChoiceField } from './name-choice-field';
import { ReviewTextField } from './review-text-field';
import { StarRatingInput } from './star-rating-input';

interface ReviewFormProps {
  /** Sends the review: the portal's action or the invitation link's. */
  send: (input: ReviewInput) => Promise<ActionResult<SubmittedReview>>;
  onDone: (review: SubmittedReview) => void;
  /** What `first_name` would show, when the page knows it. */
  firstName?: string | null;
}

/**
 * Stars, optional words and the name to show: the one review form, for the portal and for the
 * invitation link. It asks for no consent: writing a review is the customer's own act.
 */
export function ReviewForm({ send, onDone, firstName }: ReviewFormProps) {
  const text = reviewForm;
  const [rating, setRating] = useState(0);
  const [words, setWords] = useState('');
  const [choice, setChoice] = useState<ReviewNameChoice>('first_name');
  const [local, setLocal] = useState<string>();
  const { errors, message, pending, submit } = useFormSubmit({ conflict: text.unreachable, unreachable: text.unreachable });

  function sendReview(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (rating < 1) {
      setLocal(text.ratingRequired);
      return;
    }

    setLocal(undefined);
    submit(() => send({ rating, text: words.trim() || undefined, display_name: choice }), onDone);
  }

  return (
    <form onSubmit={sendReview} className="space-y-5" noValidate>
      <StarRatingInput
        value={rating}
        error={local ?? errors.rating}
        onChange={(next) => {
          setLocal(undefined);
          setRating(next);
        }}
      />
      <ReviewTextField value={words} error={errors.text} onChange={setWords} />
      <NameChoiceField value={choice} firstName={firstName} error={errors.display_name} onChange={setChoice} />

      <p className="text-xs text-site-muted">{text.moderationNote}</p>

      <div className="flex flex-wrap items-center gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? text.submitting : text.submit}
        </Button>
        <FormStatus message={message ?? errors.visit_id} savedText={text.doneTitle} />
      </div>
    </form>
  );
}

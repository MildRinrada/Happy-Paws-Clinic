'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { publishReviewAction } from '../../actions/moderate-review-actions';
import { staffReviewsText } from '../../content/staff-reviews';
import type { StaffReview } from '../../types/review.types';

/** Puts a pending review on the public site, in the customer's own words. */
export function PublishReviewButton({ review, onChange }: { review: StaffReview; onChange: (review: StaffReview) => void }) {
  const text = staffReviewsText;
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function publish() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await publishReviewAction({ id: review.id, version: review.version });
        if (result.ok) onChange(result.data);
        else setError(result.error.code === 'version_conflict' || result.error.code === 'state_conflict' ? text.conflict : result.error.message);
      } catch {
        setError(text.unreachable);
      }
    });
  }

  return (
    <div className="space-y-1">
      <Button size="sm" disabled={pending} onClick={publish}>
        {pending ? text.publishing : text.publish}
      </Button>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}

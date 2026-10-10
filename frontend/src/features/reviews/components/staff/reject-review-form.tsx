'use client';

import { useId, useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Select } from '@/components/ui/field';
import { rejectReviewAction } from '../../actions/moderate-review-actions';
import { rejectReasonLabels, staffReviewsText } from '../../content/staff-reviews';
import type { RejectReason, StaffReview } from '../../types/review.types';

interface RejectReviewFormProps {
  review: StaffReview;
  onChange: (review: StaffReview) => void;
}

/**
 * Keeps a review off the site, or takes a published one down, for a reason from the fixed list.
 * There is no other reason and no free text: that is what keeps the published average honest.
 */
export function RejectReviewForm({ review, onChange }: RejectReviewFormProps) {
  const text = staffReviewsText.reject;
  const id = useId();
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState<RejectReason | ''>('');
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function reject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason) {
      setError(text.chooseFirst);
      return;
    }

    setError(undefined);
    startTransition(async () => {
      try {
        const result = await rejectReviewAction({ id: review.id, version: review.version, reason });
        if (result.ok) onChange(result.data);
        else
          setError(
            result.error.code === 'version_conflict' || result.error.code === 'state_conflict'
              ? staffReviewsText.conflict
              : result.error.message,
          );
      } catch {
        setError(staffReviewsText.unreachable);
      }
    });
  }

  if (!open) {
    return (
      <Button size="sm" variant="danger" onClick={() => setOpen(true)}>
        {review.status === 'published' ? text.takeDown : text.open}
      </Button>
    );
  }

  return (
    <form onSubmit={reject} className="w-full space-y-3 rounded-lg border border-red-100 bg-red-50/40 p-3">
      <Field label={text.reason} htmlFor={id} error={error} hint={text.note}>
        <Select id={id} value={reason} onChange={(event) => setReason(event.target.value as RejectReason | '')}>
          <option value="">{text.choose}</option>
          {(Object.keys(rejectReasonLabels) as RejectReason[]).map((value) => (
            <option key={value} value={value}>
              {rejectReasonLabels[value]}
            </option>
          ))}
        </Select>
      </Field>
      <div className="flex flex-wrap gap-2">
        <Button type="submit" size="sm" variant="danger" disabled={pending}>
          {pending ? text.confirming : text.confirm}
        </Button>
        <Button size="sm" variant="ghost" disabled={pending} onClick={() => setOpen(false)}>
          {text.cancel}
        </Button>
      </div>
    </form>
  );
}

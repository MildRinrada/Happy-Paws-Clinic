'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { formatDateTime } from '@/lib/format/datetime';
import { replyToReviewAction } from '../../actions/moderate-review-actions';
import { REVIEW_TEXT_MAX } from '../../content/review-form';
import { staffReviewsText } from '../../content/staff-reviews';
import type { StaffReview } from '../../types/review.types';

interface ReplyReviewFormProps {
  review: StaffReview;
  onChange: (review: StaffReview) => void;
}

/** The clinic's one public answer under a review: write it, or rewrite it. The customer's words stay as they are. */
export function ReplyReviewForm({ review, onChange }: ReplyReviewFormProps) {
  const text = staffReviewsText.reply;
  const id = useId();
  const [open, setOpen] = useState(false);
  const [words, setWords] = useState(review.reply?.text ?? '');
  const { errors, message, saved, pending, submit, touch } = useFormSubmit({
    conflict: staffReviewsText.conflict,
    unreachable: staffReviewsText.unreachable,
  });

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () => replyToReviewAction({ id: review.id, version: review.version, text: words }),
      (updated) => {
        onChange(updated);
        setOpen(false);
      },
    );
  }

  return (
    <div className="space-y-2">
      {review.reply && !open && (
        <div className="rounded-lg bg-ink-50 px-3 py-2 text-sm text-ink-800">
          <p className="text-xs font-semibold text-ink-600">
            {text.heading} | {text.repliedAt(formatDateTime(review.reply.replied_at))}
          </p>
          <p className="mt-1 whitespace-pre-line break-words">{review.reply.text}</p>
        </div>
      )}

      {open ? (
        <form onSubmit={save} onChange={touch} className="space-y-3">
          <Field label={text.label} htmlFor={id} error={errors.text}>
            <Textarea id={id} rows={3} value={words} maxLength={REVIEW_TEXT_MAX} onChange={(event) => setWords(event.target.value)} />
          </Field>
          <div className="flex flex-wrap items-center gap-2">
            <Button type="submit" size="sm" disabled={pending || words.trim() === ''}>
              {pending ? text.saving : text.save}
            </Button>
            <Button size="sm" variant="ghost" disabled={pending} onClick={() => setOpen(false)}>
              {text.cancel}
            </Button>
            <FormStatus message={message} saved={saved} savedText={text.saved} />
          </div>
        </form>
      ) : (
        <Button size="sm" variant="secondary" onClick={() => setOpen(true)}>
          {review.reply ? text.edit : text.write}
        </Button>
      )}
    </div>
  );
}

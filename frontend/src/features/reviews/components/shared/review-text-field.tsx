'use client';

import { useId } from 'react';
import { REVIEW_TEXT_MAX, reviewForm } from '../../content/review-form';

interface ReviewTextFieldProps {
  value: string;
  error?: string;
  onChange: (text: string) => void;
}

/** The optional words of a review, with how much room is left. Plain text only. */
export function ReviewTextField({ value, error, onChange }: ReviewTextFieldProps) {
  const id = useId();
  const text = reviewForm;

  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-sm font-semibold text-ink-900">
        {text.text}
      </label>
      <textarea
        id={id}
        rows={5}
        value={value}
        maxLength={REVIEW_TEXT_MAX}
        aria-invalid={Boolean(error)}
        aria-describedby={`${id}-hint`}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-xl border border-ink-200 bg-white px-3.5 py-2.5 text-sm text-ink-900 placeholder:text-ink-400 focus:border-site-teal aria-[invalid=true]:border-red-400"
      />
      <div id={`${id}-hint`} className="flex flex-wrap justify-between gap-x-3 text-xs text-site-muted">
        <span>{text.textHint}</span>
        <span>{text.textCount(value.length, REVIEW_TEXT_MAX)}</span>
      </div>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}

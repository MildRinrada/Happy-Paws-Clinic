'use client';

import { useId } from 'react';
import { reviewForm } from '../../content/review-form';

interface StarRatingInputProps {
  value: number;
  error?: string;
  onChange: (rating: number) => void;
}

/**
 * Five stars to pick a score from. Real radio buttons under the stars, so a keyboard and a screen
 * reader choose the same way a finger does; each star is big enough to hit on a phone.
 */
export function StarRatingInput({ value, error, onChange }: StarRatingInputProps) {
  const name = useId();
  const errorId = `${name}-error`;
  const text = reviewForm;

  return (
    <fieldset aria-describedby={error ? errorId : undefined} className="space-y-1.5">
      <legend className="text-sm font-semibold text-ink-900">{text.rating}</legend>

      <div className="flex flex-wrap items-center gap-1">
        {[1, 2, 3, 4, 5].map((star) => (
          <label key={star} className="cursor-pointer rounded-lg p-1 has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-site-teal">
            <input
              type="radio"
              name={name}
              value={star}
              checked={value === star}
              onChange={() => onChange(star)}
              className="sr-only"
            />
            <span className="sr-only">{text.starLabel(star)}</span>
            <svg aria-hidden="true" viewBox="0 0 20 20" className={`size-9 ${star <= value ? 'fill-amber-400' : 'fill-ink-200'}`}>
              <path d="M10 1.5l2.6 5.3 5.9.9-4.3 4.1 1 5.8L10 14.9l-5.2 2.7 1-5.8L1.5 7.7l5.9-.9L10 1.5z" />
            </svg>
          </label>
        ))}
        {value > 0 && <span className="ml-2 text-sm text-site-muted">{text.starWords[value]}</span>}
      </div>

      {error ? (
        <p id={errorId} role="alert" className="text-sm text-red-700">
          {error}
        </p>
      ) : (
        <p className="text-xs text-site-muted">{text.ratingHint}</p>
      )}
    </fieldset>
  );
}

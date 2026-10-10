'use client';

import { useId } from 'react';
import { reviewForm } from '../../content/review-form';
import type { ReviewNameChoice } from '../../types/review.types';

interface NameChoiceFieldProps {
  value: ReviewNameChoice;
  /** What `first_name` would show, when the page knows it. The invitation page does not say. */
  firstName?: string | null;
  error?: string;
  onChange: (choice: ReviewNameChoice) => void;
}

/** Which name the public page shows: the first name only, or "ลูกค้า". Never a surname. */
export function NameChoiceField({ value, firstName, error, onChange }: NameChoiceFieldProps) {
  const name = useId();
  const text = reviewForm;
  const choices: ReviewNameChoice[] = ['first_name', 'anonymous'];

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-semibold text-ink-900">{text.name}</legend>

      {choices.map((choice) => (
        <label key={choice} className="flex items-start gap-2.5 text-sm text-ink-800">
          <input
            type="radio"
            name={name}
            value={choice}
            checked={value === choice}
            onChange={() => onChange(choice)}
            className="mt-0.5 size-4 shrink-0 accent-site-teal"
          />
          <span className="min-w-0">
            {text.nameChoices[choice]}
            {choice === 'first_name' && firstName && (
              <span className="block text-xs text-site-muted">{text.firstNameAs(firstName)}</span>
            )}
          </span>
        </label>
      ))}

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </fieldset>
  );
}

import type { InputHTMLAttributes } from 'react';

interface SignInFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  id: string;
  label: string;
  hint?: string;
  /** A refusal about this field. Shown in place of the hint, and marks the input invalid. */
  error?: string;
}

/** A label above a roomy input, in the public site's style rather than the console's. */
export function SignInField({ id, label, hint, error, className = '', ...input }: SignInFieldProps) {
  const noteId = error ? `${id}-error` : hint ? `${id}-hint` : undefined;

  return (
    <div className="space-y-2">
      <label htmlFor={id} className="block text-[15px] text-ink-700">
        {label}
      </label>
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={noteId}
        className={`h-11 w-full min-w-0 rounded-[10px] border border-ink-200 bg-white px-3.5 text-[15px] text-ink-900 placeholder:text-ink-400 focus:border-site-teal aria-[invalid=true]:border-red-400 ${className}`}
        {...input}
      />
      {error ? (
        <p id={noteId} className="text-sm text-red-700">
          {error}
        </p>
      ) : (
        hint && (
          <p id={noteId} className="text-sm text-site-muted">
            {hint}
          </p>
        )
      )}
    </div>
  );
}

import type { InputHTMLAttributes } from 'react';

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  id: string;
  label: string;
  hint?: string;
  error?: string;
}

/** A labelled one-line input in the portal's style, with a hint or an error under it. */
export function TextField({ id, label, hint, error, className = '', ...input }: TextFieldProps) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-sm font-semibold text-ink-900">
        {label}
      </label>
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        className={`h-11 w-full rounded-xl border border-ink-200 bg-white px-3.5 text-sm text-ink-900 placeholder:text-ink-400 focus:border-site-teal aria-[invalid=true]:border-red-400 ${className}`}
        {...input}
      />
      {error ? (
        <p role="alert" className="text-xs text-red-700">
          {error}
        </p>
      ) : (
        hint && <p className="text-xs text-site-muted">{hint}</p>
      )}
    </div>
  );
}

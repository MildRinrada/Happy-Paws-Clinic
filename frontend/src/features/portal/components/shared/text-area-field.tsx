import type { TextareaHTMLAttributes } from 'react';

interface TextAreaFieldProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  id: string;
  label: string;
  hint?: string;
}

/** A labelled box for a few lines of text, with a hint under it. */
export function TextAreaField({ id, label, hint, className = '', ...textarea }: TextAreaFieldProps) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-sm font-semibold text-ink-900">
        {label}
      </label>
      <textarea
        id={id}
        className={`w-full rounded-xl border border-ink-200 bg-white px-3.5 py-2.5 text-sm text-ink-900 placeholder:text-ink-400 focus:border-site-teal ${className}`}
        {...textarea}
      />
      {hint && <p className="text-xs text-site-muted">{hint}</p>}
    </div>
  );
}

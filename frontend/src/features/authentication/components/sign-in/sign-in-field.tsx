import type { InputHTMLAttributes } from 'react';

interface SignInFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  id: string;
  label: string;
  hint?: string;
}

/** A label above a roomy input, in the public site's style rather than the console's. */
export function SignInField({ id, label, hint, className = '', ...input }: SignInFieldProps) {
  return (
    <div className="space-y-2">
      <label htmlFor={id} className="block text-[15px] text-ink-700">
        {label}
      </label>
      <input
        id={id}
        className={`h-11 w-full rounded-[10px] border border-ink-200 bg-white px-3.5 text-[15px] text-ink-900 placeholder:text-ink-400 focus:border-site-teal ${className}`}
        {...input}
      />
      {hint && <p className="text-sm text-site-muted">{hint}</p>}
    </div>
  );
}

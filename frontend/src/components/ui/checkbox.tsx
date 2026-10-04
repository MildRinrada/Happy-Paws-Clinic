import type { InputHTMLAttributes, ReactNode } from 'react';

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: ReactNode;
  /** A line of smaller text under the label. */
  hint?: ReactNode;
}

/** A checkbox with its label beside it. The whole row is the click target. */
export function Checkbox({ label, hint, className = '', ...props }: CheckboxProps) {
  return (
    <label className={`flex cursor-pointer items-start gap-2.5 text-sm text-ink-800 ${className}`}>
      <input type="checkbox" className="mt-0.5 size-4 shrink-0 accent-brand-600" {...props} />
      <span>
        {label}
        {hint && <span className="block text-xs text-ink-500">{hint}</span>}
      </span>
    </label>
  );
}

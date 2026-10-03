import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react';

const control =
  'h-10 w-full rounded-lg border border-ink-200 bg-white px-3 text-sm text-ink-900 ' +
  'placeholder:text-ink-400 disabled:bg-ink-50 disabled:text-ink-500 ' +
  'aria-[invalid=true]:border-red-400';

interface FieldProps {
  label: string;
  /** Shown under the control and announced with it. Marks the control invalid. */
  error?: string;
  hint?: string;
  children: ReactNode;
  htmlFor: string;
}

/** A labelled control. The label is always visible: a placeholder is not a label at a busy desk. */
export function Field({ label, error, hint, children, htmlFor }: FieldProps) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={htmlFor} className="block text-sm font-medium text-ink-700">
        {label}
      </label>
      {children}
      {error ? (
        <p id={`${htmlFor}-error`} role="alert" className="text-sm text-red-700">
          {error}
        </p>
      ) : (
        hint && <p className="text-sm text-ink-500">{hint}</p>
      )}
    </div>
  );
}

export function Input({ className = '', ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={`${control} ${className}`} {...props} />;
}

export function Select({ className = '', ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={`${control} ${className}`} {...props} />;
}

/** Same look as `Input`, for text that runs to more than a line. Grows by hand, not by itself. */
export function Textarea({ className = '', rows = 3, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea rows={rows} className={`${control} h-auto resize-y py-2 leading-relaxed ${className}`} {...props} />;
}

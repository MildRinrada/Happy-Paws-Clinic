import { useId } from 'react';

interface Props {
  label: string;
  hint: string;
  value: string;
  error?: string;
  required?: boolean;
  onChange: (value: string) => void;
}

/**
 * A date with its label, a hint under it and, when there is one, what is wrong with it. The phone's
 * own date picker opens on tap. No `name`, on purpose: the dates never go into a request.
 */
export function DateField({ label, hint, value, error, required, onChange }: Props) {
  const id = useId();
  const hintId = `${id}-hint`;
  const errorId = `${id}-error`;

  return (
    <div>
      <label htmlFor={id} className="block text-[15px] font-semibold">
        {label}
      </label>
      <p id={hintId} className="mt-0.5 text-[13px] text-site-muted">
        {hint}
      </p>
      <input
        id={id}
        type="date"
        value={value}
        required={required}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${hintId} ${errorId}` : hintId}
        onChange={(event) => onChange(event.target.value)}
        className={`mt-2 h-12 w-full rounded-xl border bg-white px-4 text-base outline-none transition focus:ring-2 focus:ring-site-teal/40 ${
          error ? 'border-site-alert' : 'border-site-border focus:border-site-teal'
        }`}
      />
      {error && (
        <p id={errorId} className="mt-1.5 text-sm font-semibold text-site-alert">
          {error}
        </p>
      )}
    </div>
  );
}

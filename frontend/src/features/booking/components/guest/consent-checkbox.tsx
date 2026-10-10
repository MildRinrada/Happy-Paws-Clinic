import { useId, type ReactNode } from 'react';

interface ConsentCheckboxProps {
  name: string;
  children: ReactNode;
  /** A refusal about this box, e.g. the privacy notice left unticked. */
  error?: string;
}

/** A tick box with its sentence beside it, and the refusal under it when there is one. */
export function ConsentCheckbox({ name, children, error }: ConsentCheckboxProps) {
  const id = useId();
  const errorId = `${id}-error`;

  return (
    <div className="space-y-1">
      <div className="flex items-start gap-3">
        <input
          id={id}
          name={name}
          type="checkbox"
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          className="mt-1 size-4 shrink-0 accent-site-teal"
        />
        <label htmlFor={id} className="text-sm text-ink-700">
          {children}
        </label>
      </div>
      {error && (
        <p id={errorId} className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}

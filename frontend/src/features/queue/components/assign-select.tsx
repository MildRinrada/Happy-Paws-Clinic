'use client';

interface Option {
  value: string;
  label: string;
}

interface Props {
  /** Read by screen readers only: the card around it already says which ticket this is. */
  label: string;
  placeholder: string;
  value: string;
  options: Option[];
  disabled: boolean;
  onPick: (value: string) => void;
}

/** A compact select on a ticket that saves as soon as something is picked. */
export function AssignSelect({ label, placeholder, value, options, disabled, onPick }: Props) {
  return (
    <label className="flex items-center gap-1.5 text-sm text-ink-600">
      <span className="sr-only">{label}</span>
      <select
        className="h-8 max-w-40 rounded-lg border border-ink-200 bg-white px-2 text-sm"
        value={value}
        disabled={disabled}
        onChange={(event) => {
          if (event.target.value) {
            onPick(event.target.value);
          }
        }}
      >
        <option value="">{placeholder}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}

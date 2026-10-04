import { Checkbox } from './checkbox';

interface CheckboxGroupProps {
  legend: string;
  hint?: string;
  error?: string;
  /** Value to label, in the order to show them. */
  options: Record<string, string>;
  selected: string[];
  onChange: (selected: string[]) => void;
}

/** A set of on/off choices sent as one list. The list keeps the options' own order. */
export function CheckboxGroup({ legend, hint, error, options, selected, onChange }: CheckboxGroupProps) {
  const values = Object.keys(options);

  function toggle(value: string, on: boolean) {
    const next = new Set(selected);
    if (on) next.add(value);
    else next.delete(value);
    onChange(values.filter((option) => next.has(option)));
  }

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium text-ink-700">{legend}</legend>
      {hint && <p className="text-sm text-ink-500">{hint}</p>}
      <div className="grid gap-2 sm:grid-cols-2">
        {values.map((value) => (
          <Checkbox key={value} label={options[value]} checked={selected.includes(value)} onChange={(event) => toggle(value, event.target.checked)} />
        ))}
      </div>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </fieldset>
  );
}

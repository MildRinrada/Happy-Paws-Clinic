import { Field, Input } from '@/components/ui/field';

interface NumberFieldProps {
  id: string;
  label: string;
  hint?: string;
  error?: string;
  value: number;
  min: number;
  max: number;
  onChange: (value: number) => void;
}

/** A whole number in a narrow box. An emptied box reads as zero rather than as "not a number". */
export function NumberField({ id, label, hint, error, value, min, max, onChange }: NumberFieldProps) {
  return (
    <Field label={label} htmlFor={id} hint={hint} error={error}>
      <Input
        id={id}
        type="number"
        step={1}
        min={min}
        max={max}
        value={value}
        required
        className="max-w-32"
        onChange={(event) => onChange(Math.trunc(Number(event.target.value) || 0))}
      />
    </Field>
  );
}

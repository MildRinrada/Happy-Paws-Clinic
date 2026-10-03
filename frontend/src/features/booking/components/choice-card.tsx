import type { ReactNode } from 'react';

interface ChoiceCardProps {
  name: string;
  value: string;
  checked: boolean;
  onChange: (value: string) => void;
  title: ReactNode;
  detail?: ReactNode;
}

/** A radio button drawn as a card: big enough to hit with a thumb, with a line of detail under it. */
export function ChoiceCard({ name, value, checked, onChange, title, detail }: ChoiceCardProps) {
  return (
    <label
      className={`flex cursor-pointer items-start gap-3 rounded-xl border p-3.5 transition ${
        checked ? 'border-site-teal bg-brand-50' : 'border-ink-200 bg-white hover:border-ink-300'
      }`}
    >
      <input
        type="radio"
        name={name}
        value={value}
        checked={checked}
        onChange={() => onChange(value)}
        className="mt-1 accent-site-teal"
      />
      <span className="min-w-0">
        <span className="block text-sm font-semibold text-ink-900">{title}</span>
        {detail && <span className="mt-0.5 block text-xs text-site-muted">{detail}</span>}
      </span>
    </label>
  );
}

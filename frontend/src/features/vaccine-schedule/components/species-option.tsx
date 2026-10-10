import type { Species } from '../types/vaccine-schedule.types';

interface Props {
  name: string;
  value: Species;
  label: string;
  detail: string;
  checked: boolean;
  onChange: (value: Species) => void;
}

/** One choice of dog or cat, drawn as a card big enough to hit with a thumb. */
export function SpeciesOption({ name, value, label, detail, checked, onChange }: Props) {
  return (
    <label className="flex min-h-14 cursor-pointer items-center gap-3 rounded-xl border border-site-border bg-white px-4 py-3 transition hover:border-site-teal has-[:checked]:border-site-teal has-[:checked]:bg-brand-50 has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-site-teal/40">
      <input
        type="radio"
        name={name}
        value={value}
        checked={checked}
        onChange={() => onChange(value)}
        className="size-5 shrink-0 accent-site-teal"
      />
      <span>
        <span className="block text-base font-bold leading-tight">{label}</span>
        <span className="block text-[13px] text-site-muted">{detail}</span>
      </span>
    </label>
  );
}

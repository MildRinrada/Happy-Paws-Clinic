'use client';

interface SwitchControlProps {
  checked: boolean;
  label: string;
  disabled?: boolean;
  onToggle: () => void;
}

/** A small on and off switch. The label is for screen readers; the card already says what it switches. */
export function SwitchControl({ checked, label, disabled, onToggle }: SwitchControlProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={onToggle}
      className={[
        'relative inline-flex h-5 w-9 shrink-0 items-center rounded-full transition-colors disabled:cursor-not-allowed disabled:opacity-60',
        checked ? 'bg-brand-600' : 'bg-ink-300',
      ].join(' ')}
    >
      <span className={['inline-block size-4 rounded-full bg-white shadow transition-transform', checked ? 'translate-x-4' : 'translate-x-0.5'].join(' ')} />
    </button>
  );
}

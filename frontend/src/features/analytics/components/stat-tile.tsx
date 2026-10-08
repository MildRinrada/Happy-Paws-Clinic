import type { ReactNode } from 'react';

interface StatTileProps {
  label: string;
  value: ReactNode;
  /** A short line under the value: what it is counted from, or a second figure. */
  hint?: ReactNode;
  /** The headline tile of a group reads a size larger. */
  emphasis?: boolean;
}

/** One number with its label. Text stays in ink colours; nothing here relies on colour to mean something. */
export function StatTile({ label, value, hint, emphasis = false }: StatTileProps) {
  return (
    <div className="rounded-lg border border-ink-100 bg-ink-50/40 px-3 py-2.5">
      <p className="text-xs text-ink-500">{label}</p>
      <p className={`font-semibold tabular-nums text-ink-900 ${emphasis ? 'text-xl' : 'text-base'}`}>{value}</p>
      {hint && <p className="mt-0.5 text-xs text-ink-500">{hint}</p>}
    </div>
  );
}

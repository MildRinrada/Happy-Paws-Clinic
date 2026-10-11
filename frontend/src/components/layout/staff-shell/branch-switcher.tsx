'use client';

import { useTransition } from 'react';
import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import { BranchDot } from './branch-dot';

interface Props {
  branches: { id: string; name: string }[];
  current?: string;
  /** Bound on the server; the branch list it is checked against is the server's too. */
  onSelect: (branchId: string) => Promise<void>;
}

/**
 * Which branch the console is showing, with the branch's colour dot beside it. Drawn as plain text
 * when there is only one to show.
 */
export function BranchSwitcher({ branches, current, onSelect }: Props) {
  const [pending, startTransition] = useTransition();
  const text = staffShellText.branch;
  const shown = branches.find((branch) => branch.id === current) ?? branches[0];

  if (branches.length <= 1) {
    return (
      <span className="flex min-w-0 items-center gap-2 text-sm text-ink-700">
        {shown && <BranchDot branchId={shown.id} />}
        <span className="truncate">{shown?.name ?? text.none}</span>
      </span>
    );
  }

  return (
    <label className="flex min-w-0 items-center gap-2 text-sm text-ink-600">
      <span className="sr-only">{text.label}</span>
      {shown && <BranchDot branchId={shown.id} />}
      <select
        className="h-9 min-w-0 rounded-lg border border-ink-200 bg-white px-2 text-sm text-ink-900"
        value={current}
        disabled={pending}
        onChange={(event) => startTransition(() => onSelect(event.target.value))}
      >
        {branches.map((branch) => (
          <option key={branch.id} value={branch.id}>
            {branch.name}
          </option>
        ))}
      </select>
    </label>
  );
}

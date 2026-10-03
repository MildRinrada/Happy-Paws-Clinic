'use client';

import { useTransition } from 'react';

interface Props {
  branches: { id: string; name: string }[];
  current?: string;
  /** Bound on the server; the branch list it is checked against is the server's too. */
  onSelect: (branchId: string) => Promise<void>;
}

/** Which branch the console is showing. Drawn as plain text when there is only one to show. */
export function BranchSwitcher({ branches, current, onSelect }: Props) {
  const [pending, startTransition] = useTransition();

  if (branches.length <= 1) {
    return <span className="text-sm text-ink-600">{branches[0]?.name ?? 'ยังไม่มีสาขา'}</span>;
  }

  return (
    <label className="flex items-center gap-2 text-sm text-ink-600">
      <span>สาขา</span>
      <select
        className="h-9 rounded-lg border border-ink-200 bg-white px-2 text-sm text-ink-900"
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

import type { ReactNode } from 'react';

/** What a list shows when it has nothing in it — said plainly, so empty never looks like broken. */
export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="rounded-xl border border-dashed border-ink-200 px-6 py-10 text-center">
      <p className="font-medium text-ink-700">{title}</p>
      {children && <p className="mt-1 text-sm text-ink-500">{children}</p>}
    </div>
  );
}

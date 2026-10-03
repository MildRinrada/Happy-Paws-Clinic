import type { ReactNode } from 'react';

/** One section inside a visit: a small heading and its lines. */
export function RecordSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div>
      <h3 className="text-xs font-semibold text-site-muted">{title}</h3>
      <div className="mt-1 text-sm text-ink-900">{children}</div>
    </div>
  );
}

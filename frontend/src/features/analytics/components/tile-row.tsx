import type { ReactNode } from 'react';

/** A row of stat tiles that wraps on a narrow screen. */
export function TileRow({ children }: { children: ReactNode }) {
  return <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-6">{children}</div>;
}

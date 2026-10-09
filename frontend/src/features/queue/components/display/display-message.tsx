import type { ReactNode } from 'react';

/** A single line in the middle of the screen, for when there is no board to show. */
export function DisplayMessage({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div role="alert" className="flex h-dvh flex-col items-center justify-center gap-[2vh] p-[4vh] text-center">
      <p className="font-display text-[clamp(1.5rem,5vh,4.5rem)] font-semibold">{title}</p>
      {children && <div className="text-[clamp(1rem,3vh,2.5rem)] text-ink-300">{children}</div>}
    </div>
  );
}

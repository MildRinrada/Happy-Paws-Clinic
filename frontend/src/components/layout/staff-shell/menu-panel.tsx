import type { HTMLAttributes, ReactNode, Ref } from 'react';

interface MenuPanelProps extends HTMLAttributes<HTMLDivElement> {
  ref: Ref<HTMLDivElement>;
  children: ReactNode;
}

/** The card a top-bar menu opens in, below its button on the right. */
export function MenuPanel({ ref, className = 'w-60', children, ...props }: MenuPanelProps) {
  return (
    <div
      ref={ref}
      className={`absolute right-0 top-full z-40 mt-2 rounded-xl border border-ink-200 bg-white p-1.5 shadow-lg ${className}`}
      {...props}
    >
      {children}
    </div>
  );
}

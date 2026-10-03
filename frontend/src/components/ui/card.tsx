import type { ReactNode } from 'react';

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <section className={`rounded-xl border border-ink-200 bg-white ${className}`}>{children}</section>;
}

export function CardHeader({ title, aside }: { title: ReactNode; aside?: ReactNode }) {
  return (
    <header className="flex items-center justify-between gap-4 border-b border-ink-100 px-5 py-3.5">
      <h2 className="font-display text-base font-semibold text-ink-900">{title}</h2>
      {aside}
    </header>
  );
}

export function CardBody({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <div className={`px-5 py-4 ${className}`}>{children}</div>;
}

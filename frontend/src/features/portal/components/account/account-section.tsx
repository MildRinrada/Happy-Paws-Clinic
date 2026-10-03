import type { ReactNode } from 'react';

/** One white block of the account page: a title, an optional line under it, then its form. */
export function AccountSection({ title, intro, children }: { title: string; intro?: string; children: ReactNode }) {
  return (
    <section className="space-y-4 rounded-2xl bg-white p-5 shadow-sm sm:p-6">
      <div>
        <h2 className="text-base font-semibold text-ink-900">{title}</h2>
        {intro && <p className="mt-1 text-sm text-site-muted">{intro}</p>}
      </div>
      {children}
    </section>
  );
}

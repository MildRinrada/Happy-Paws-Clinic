import type { Metadata } from 'next';
import type { ReactNode } from 'react';

// A live screen of today's queue. Never prerendered.
export const dynamic = 'force-dynamic';

export const metadata: Metadata = { robots: { index: false, follow: false } };

/**
 * The waiting-room screen that runs on its own display token: `/display`, `/display/setup` and
 * the `/display/stream` route handler (docs/contract-extensions.md § 30).
 *
 * Deliberately no staff session here. Nothing in this group reads `hp_at`, and the middleware does
 * not match `/display`, so a TV set up this way never holds a staff account and cannot reach the
 * console from its keyboard. The old `/queue/display` in `(display)` still works with a session.
 */
export default function ScreenLayout({ children }: { children: ReactNode }) {
  return <div className="min-h-dvh bg-ink-900 text-white">{children}</div>;
}

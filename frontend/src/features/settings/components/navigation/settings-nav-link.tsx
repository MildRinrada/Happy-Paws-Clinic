'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { LinkPendingDot } from '@/components/layout/link-pending-dot';
import { isCurrentTab } from '../../lib/is-current-tab';
import type { VisibleSettingsTab } from '../../types/settings-nav.types';
import { AttentionDot } from './attention-dot';

/** One page in the side list. The current one is filled in, and says so to a screen reader. */
export function SettingsNavLink({ tab }: { tab: VisibleSettingsTab }) {
  const current = isCurrentTab(usePathname(), tab.href);

  return (
    <Link
      href={tab.href}
      aria-current={current ? 'page' : undefined}
      className={`flex items-center gap-2 rounded-lg px-3 py-2 text-sm transition-colors ${
        current ? 'bg-brand-50 font-medium text-brand-800 ring-1 ring-brand-200' : 'text-ink-700 hover:bg-ink-100'
      }`}
    >
      <span className="min-w-0 flex-1">{tab.label}</span>
      {tab.attention && <AttentionDot />}
      <LinkPendingDot />
    </Link>
  );
}

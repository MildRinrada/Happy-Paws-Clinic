'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState, type FocusEvent, type PointerEvent } from 'react';
import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import type { VisibleNavItem } from '@/features/staff-shell/types/staff-shell.types';
import { LinkPendingDot } from '../link-pending-dot';
import { CollapsedTooltip } from './collapsed-tooltip';
import { NavBadge } from './nav-badge';
import { useNavCounts } from './nav-counts-context';
import { StaffIcon } from './staff-icon';

interface NavItemLinkProps {
  item: VisibleNavItem;
  /** Called when the link is followed, so the phone drawer can close. */
  onNavigate?: () => void;
}

/**
 * One menu entry: its icon, its name, and the count of work waiting behind it. The current page has
 * a soft mint background and an accent bar on the left, and says so to a screen reader.
 *
 * Inside a folded sidebar (`data-collapsed` on the aside) the name is kept for screen readers only
 * and shown as a tooltip on hover or focus.
 */
export function NavItemLink({ item, onNavigate }: NavItemLinkProps) {
  const pathname = usePathname();
  const counts = useNavCounts();
  const [tip, setTip] = useState<{ top: number; left: number } | null>(null);

  const current = pathname === item.section || pathname.startsWith(`${item.section}/`);
  const count = item.badge ? counts[item.badge] : undefined;

  const showTip = (event: PointerEvent<HTMLAnchorElement> | FocusEvent<HTMLAnchorElement>) => {
    if (!event.currentTarget.closest('[data-collapsed="true"]')) return;

    const box = event.currentTarget.getBoundingClientRect();
    setTip({ top: box.top + box.height / 2, left: box.right + 10 });
  };
  const hideTip = () => setTip(null);

  return (
    <Link
      href={item.href}
      aria-current={current ? 'page' : undefined}
      // The badge's number is drawn for the eye; a screen reader hears it in words with the name.
      aria-label={item.badge && count ? `${item.label}, ${staffShellText.badgeLabel(item.badge, count)}` : undefined}
      onClick={() => {
        hideTip();
        onNavigate?.();
      }}
      onPointerEnter={showTip}
      onPointerLeave={hideTip}
      onFocus={showTip}
      onBlur={hideTip}
      className={`relative flex items-center gap-3 rounded-lg py-2 pl-3 pr-2 text-sm font-medium transition-colors group-data-[collapsed=true]/sidebar:justify-center group-data-[collapsed=true]/sidebar:px-0 ${
        current ? 'bg-brand-50 text-brand-800' : 'text-ink-700 hover:bg-ink-100 hover:text-ink-900'
      }`}
    >
      {current && <span aria-hidden className="absolute inset-y-1.5 left-0 w-1 rounded-full bg-brand-600" />}
      <StaffIcon name={item.icon} className={`size-5 shrink-0 ${current ? 'text-brand-700' : 'text-ink-500'}`} />
      <span className="min-w-0 truncate group-data-[collapsed=true]/sidebar:sr-only">{item.label}</span>
      <span className="group-data-[collapsed=true]/sidebar:hidden">
        <LinkPendingDot />
      </span>
      {item.badge && count && <NavBadge kind={item.badge} count={count} />}
      {tip && <CollapsedTooltip label={item.label} top={tip.top} left={tip.left} />}
    </Link>
  );
}

import { countText } from '@/features/staff-shell/content/staff-shell-text';
import type { NavBadgeKey, NavCount } from '@/features/staff-shell/types/staff-shell.types';

/** Stock that can stop a prescription is red; the rest is ordinary work waiting. */
const tones: Record<NavBadgeKey, string> = {
  queue: 'bg-brand-600 text-white',
  messages: 'bg-amber-500 text-ink-900',
  stock: 'bg-red-600 text-white',
  lab: 'bg-sky-600 text-white',
};

/**
 * The count at the end of a menu entry. With the sidebar folded it shrinks onto the icon's corner,
 * so work waiting still shows. The number is hidden from screen readers: the link's own label says
 * it in words ("ข้อความลูกค้า, รอตอบ 3").
 */
export function NavBadge({ kind, count }: { kind: NavBadgeKey; count: NavCount }) {
  return (
    <span
      aria-hidden
      className={`ml-auto inline-flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full px-1.5 text-xs font-semibold tabular-nums ${tones[kind]} group-data-[collapsed=true]/sidebar:absolute group-data-[collapsed=true]/sidebar:right-1.5 group-data-[collapsed=true]/sidebar:top-0.5 group-data-[collapsed=true]/sidebar:ml-0 group-data-[collapsed=true]/sidebar:h-4 group-data-[collapsed=true]/sidebar:min-w-4 group-data-[collapsed=true]/sidebar:px-1 group-data-[collapsed=true]/sidebar:text-[10px] group-data-[collapsed=true]/sidebar:ring-2 group-data-[collapsed=true]/sidebar:ring-white`}
    >
      {countText(count)}
    </span>
  );
}

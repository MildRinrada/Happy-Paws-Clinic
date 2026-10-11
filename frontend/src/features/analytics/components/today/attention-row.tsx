import Link from 'next/link';
import { attentionBox } from '../../content/attention-box';
import { formatCount } from '../../lib/format-report';
import { LONG_WAIT_MINUTES } from '../../lib/today-settings';
import type { AttentionItem } from '../../types/today.types';

function labelOf(item: AttentionItem): string {
  const rows = attentionBox.rows;
  return item.key === 'long_wait' ? rows.long_wait(LONG_WAIT_MINUTES) : rows[item.key];
}

/** One line of work waiting: what, how many, and the whole line is the link to where it is done. */
export function AttentionRow({ item }: { item: AttentionItem }) {
  return (
    <li>
      <Link
        href={item.href}
        className="flex items-center justify-between gap-3 rounded-lg px-3 py-2.5 transition-colors hover:bg-amber-50 focus-visible:bg-amber-50"
      >
        <span className="min-w-0">
          <span className="block text-sm font-medium text-ink-900">{labelOf(item)}</span>
          {item.detail && <span className="block text-xs text-ink-600">{item.detail}</span>}
        </span>
        <span className="flex shrink-0 items-center gap-2">
          <span className="min-w-8 rounded-full bg-amber-100 px-2.5 py-0.5 text-center text-sm font-semibold tabular-nums text-amber-900">
            {attentionBox.count(formatCount(item.count), item.more)}
          </span>
          <span aria-hidden="true" className="text-ink-400">
            ›
          </span>
        </span>
      </Link>
    </li>
  );
}

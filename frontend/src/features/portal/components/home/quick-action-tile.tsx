import Link from 'next/link';
import type { QuickActionIcon as IconName } from '../../content/portal-home';
import { QuickActionIcon } from './quick-action-icon';

interface QuickActionTileProps {
  label: string;
  href: string;
  icon: IconName;
}

/** One shortcut: a white tile with a drawing over its name. */
export function QuickActionTile({ label, href, icon }: QuickActionTileProps) {
  return (
    <Link
      href={href}
      className="flex h-full flex-col items-center justify-center gap-1.5 rounded-2xl border border-ink-200 bg-white px-1 py-3.5 text-center transition hover:border-brand-300 hover:bg-brand-50"
    >
      <span className="text-brand-600">
        <QuickActionIcon name={icon} />
      </span>
      <span className="font-display text-[13px] leading-5 text-ink-800">{label}</span>
    </Link>
  );
}

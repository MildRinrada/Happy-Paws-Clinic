import type { VisibleNavGroup } from '@/features/staff-shell/types/staff-shell.types';
import { NavItemLink } from './nav-item-link';

/**
 * One group of the menu under its heading. With the sidebar folded the heading gives way to a thin
 * rule, and stays in the page for screen readers as the list's name.
 */
export function NavGroup({ group, onNavigate }: { group: VisibleNavGroup; onNavigate?: () => void }) {
  const headingId = `nav-group-${group.key}`;

  return (
    <div className="space-y-0.5">
      <p
        id={headingId}
        className="px-3 pb-1 pt-1 text-xs font-semibold text-ink-500 group-data-[collapsed=true]/sidebar:sr-only"
      >
        {group.label}
      </p>
      <hr aria-hidden className="mx-3 mb-2 hidden border-ink-200 group-data-[collapsed=true]/sidebar:block" />
      <ul aria-labelledby={headingId} className="space-y-0.5">
        {group.items.map((item) => (
          <li key={item.section}>
            <NavItemLink item={item} onNavigate={onNavigate} />
          </li>
        ))}
      </ul>
    </div>
  );
}

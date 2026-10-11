import { useId } from 'react';
import type { VisibleSettingsGroup } from '../../types/settings-nav.types';
import { SettingsNavLink } from './settings-nav-link';

/** A group's name over its pages. */
export function SettingsNavGroup({ group }: { group: VisibleSettingsGroup }) {
  const headingId = useId();

  return (
    <div className="space-y-1">
      <h2 id={headingId} className="px-3 text-xs font-semibold tracking-wide text-ink-500">
        {group.label}
      </h2>
      <ul aria-labelledby={headingId} className="space-y-0.5">
        {group.tabs.map((tab) => (
          <li key={tab.href}>
            <SettingsNavLink tab={tab} />
          </li>
        ))}
      </ul>
    </div>
  );
}

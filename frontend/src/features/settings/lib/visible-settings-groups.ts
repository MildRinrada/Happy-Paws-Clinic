import type { StaffContext } from '@/features/account/lib/staff-context';
import { settingsGroups } from '../content/settings-tabs';
import type { VisibleSettingsGroup } from '../types/settings-nav.types';

/**
 * The settings pages this session may use, by group, with a group left out once none of its pages
 * is. `attention` names the pages that still need setting up.
 */
export function visibleSettingsGroups(context: StaffContext | null, attention: ReadonlySet<string>): VisibleSettingsGroup[] {
  return settingsGroups.flatMap((group) => {
    const tabs = group.tabs
      .filter((tab) => context?.can(tab.scope))
      .map((tab) => ({ ...tab, attention: attention.has(tab.href) }));

    return tabs.length > 0 ? [{ key: group.key, label: group.label, tabs }] : [];
  });
}

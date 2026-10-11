import { settingsNavText } from '../../content/settings-tabs';
import type { VisibleSettingsGroup } from '../../types/settings-nav.types';
import { SettingsNavGroup } from './settings-nav-group';

/**
 * The settings pages down the left of the settings area from 1024 px, by group. It stays in view
 * while a long page scrolls. Phones get `SettingsPageSwitcher` instead.
 */
export function SettingsSideNav({ groups }: { groups: VisibleSettingsGroup[] }) {
  return (
    <nav aria-label={settingsNavText.label} className="sticky top-6 hidden space-y-5 self-start lg:block">
      {groups.map((group) => (
        <SettingsNavGroup key={group.key} group={group} />
      ))}
    </nav>
  );
}

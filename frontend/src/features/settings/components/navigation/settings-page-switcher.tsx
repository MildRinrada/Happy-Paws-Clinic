'use client';

import { usePathname, useRouter } from 'next/navigation';
import { useId } from 'react';
import { Select } from '@/components/ui/field';
import { settingsNavText } from '../../content/settings-tabs';
import { useSaveBar } from '../../hooks/use-save-bar';
import { isCurrentTab } from '../../lib/is-current-tab';
import type { VisibleSettingsGroup } from '../../types/settings-nav.types';

/**
 * Below 1024 px: one labelled list of every settings page, by group, in place of the side list. A
 * page that still needs setting up says so after its name. Leaving a form with unsaved changes asks
 * first, as a link would.
 */
export function SettingsPageSwitcher({ groups }: { groups: VisibleSettingsGroup[] }) {
  const id = useId();
  const router = useRouter();
  const pathname = usePathname();
  const { confirmLeave } = useSaveBar();
  const current = groups.flatMap((group) => group.tabs).find((tab) => isCurrentTab(pathname, tab.href));

  return (
    <div className="space-y-1.5 lg:hidden">
      <label htmlFor={id} className="block text-sm font-medium text-ink-700">
        {settingsNavText.switcher}
      </label>
      <Select
        id={id}
        value={current?.href ?? ''}
        onChange={(event) => {
          if (confirmLeave()) router.push(event.target.value);
        }}
      >
        {!current && <option value="" disabled />}
        {groups.map((group) => (
          <optgroup key={group.key} label={group.label}>
            {group.tabs.map((tab) => (
              <option key={tab.href} value={tab.href}>
                {tab.attention ? `${tab.label} (${settingsNavText.attention})` : tab.label}
              </option>
            ))}
          </optgroup>
        ))}
      </Select>
    </div>
  );
}

import type { ReactNode } from 'react';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { SettingsPageSwitcher } from '@/features/settings/components/navigation/settings-page-switcher';
import { SettingsSideNav } from '@/features/settings/components/navigation/settings-side-nav';
import { SaveBar } from '@/features/settings/components/save-bar/save-bar';
import { SaveBarProvider } from '@/features/settings/components/save-bar/save-bar-provider';
import { loadSettingsAttention } from '@/features/settings/lib/load-settings-attention';
import { visibleSettingsGroups } from '@/features/settings/lib/visible-settings-groups';

/**
 * Every settings page sits beside the list of settings pages this session can change, by group:
 * down the left from 1024 px, a single page switcher above the page on a phone. The page's forms
 * share one save bar at the foot of the content column, which also asks before unsaved changes
 * are left behind.
 */
export default async function SettingsLayout({ children }: { children: ReactNode }) {
  const context = await loadStaffContext();
  const groups = visibleSettingsGroups(context, await loadSettingsAttention(context));

  return (
    <SaveBarProvider>
      <div className="max-w-6xl lg:grid lg:grid-cols-[15rem_minmax(0,1fr)] lg:gap-8">
        {groups.length > 0 && <SettingsSideNav groups={groups} />}
        <div className="min-w-0 space-y-6 lg:col-start-2">
          {groups.length > 0 && <SettingsPageSwitcher groups={groups} />}
          {children}
          <SaveBar />
        </div>
      </div>
    </SaveBarProvider>
  );
}

import type { ReactNode } from 'react';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { SettingsTabBar } from '@/features/settings/components/settings-tab-bar';
import { settingsTabs } from '@/features/settings/content/settings-tabs';

/** Every settings page sits under one row of tabs, showing only the pages this session can change. */
export default async function SettingsLayout({ children }: { children: ReactNode }) {
  const context = await loadStaffContext();
  const tabs = settingsTabs.filter((tab) => context?.can(tab.scope));

  return (
    <div className="max-w-5xl space-y-6">
      {tabs.length > 0 && <SettingsTabBar tabs={tabs} />}
      {children}
    </div>
  );
}

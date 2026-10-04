import { redirect } from 'next/navigation';
import { Alert } from '@/components/ui/alert';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { settingsArea, settingsTabs } from '@/features/settings/content/settings-tabs';

/** `/settings` itself has nothing of its own: it opens the first page this session can change. */
export default async function SettingsIndexPage() {
  const context = await loadStaffContext();
  const first = settingsTabs.find((tab) => context?.can(tab.scope));

  if (first) {
    redirect(first.href);
  }

  return <Alert tone="info">{settingsArea.noAccess}</Alert>;
}

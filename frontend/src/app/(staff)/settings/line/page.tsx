import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { readLineSettings } from '@/features/integrations/api/lineSettings';
import { LineSettingsForm } from '@/features/integrations/components/line-settings-form';
import { LineStatusCard } from '@/features/integrations/components/line-status-card';
import { lineSettingsPage } from '@/features/integrations/content/line-settings';
import { authed } from '@/lib/auth/session';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: lineSettingsPage.metaTitle };

/** The clinic's LINE Official Account: status, credentials, and which messages go out on it. */
export default async function LineSettingsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.IntegrationManage)) {
    return <Alert tone="info">{lineSettingsPage.noAccess}</Alert>;
  }

  const settings = await authed(readLineSettings);

  return (
    <div className="space-y-6">
      <PageHeader title={lineSettingsPage.title} subtitle={lineSettingsPage.subtitle} />
      <LineStatusCard settings={settings} />
      <LineSettingsForm key={settings.version} settings={settings} />
    </div>
  );
}

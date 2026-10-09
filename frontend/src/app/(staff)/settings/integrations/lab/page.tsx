import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { readLabSettings } from '@/features/integrations/api/providerSettings';
import { LabSettingsForm } from '@/features/integrations/components/lab-settings-form';
import { ProviderNotice } from '@/features/integrations/components/provider-notice';
import { integrationsArea } from '@/features/integrations/content/integrations-nav';
import { labSettingsPage } from '@/features/integrations/content/provider-settings';
import { authed } from '@/lib/auth/session';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: labSettingsPage.metaTitle };

/** The outside lab's settings. Stored only: there is no lab connector yet. */
export default async function LabIntegrationPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.IntegrationManage)) {
    return <Alert tone="info">{integrationsArea.noAccess}</Alert>;
  }

  const settings = await authed(readLabSettings);

  return (
    <div className="space-y-6">
      <PageHeader title={labSettingsPage.title} subtitle={labSettingsPage.subtitle} />
      <ProviderNotice />
      <LabSettingsForm key={settings.version} settings={settings} />
    </div>
  );
}

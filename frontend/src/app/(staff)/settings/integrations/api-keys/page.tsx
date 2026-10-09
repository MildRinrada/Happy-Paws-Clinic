import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { ApiKeyForm } from '@/features/integrations/components/api-key-form';
import { apiKeySettingsPage } from '@/features/integrations/content/api-key-settings';
import { integrationsArea } from '@/features/integrations/content/integrations-nav';
import { apiKeyScopeOptions } from '@/features/integrations/lib/api-key-options';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: apiKeySettingsPage.metaTitle };

/** Issue a key for a machine client, shown once. */
export default async function ApiKeysPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.IntegrationManage)) {
    return <Alert tone="info">{integrationsArea.noAccess}</Alert>;
  }

  const branches = Object.fromEntries(context.branches.map((branch) => [branch.id, branch.name]));

  return (
    <div className="space-y-6">
      <PageHeader title={apiKeySettingsPage.title} subtitle={apiKeySettingsPage.subtitle} />
      <ApiKeyForm scopeOptions={apiKeyScopeOptions(context.can)} branchOptions={branches} />
    </div>
  );
}

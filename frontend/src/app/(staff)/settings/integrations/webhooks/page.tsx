import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { WebhookForm } from '@/features/integrations/components/webhook-form';
import { WebhookLogLookup } from '@/features/integrations/components/webhook-log-lookup';
import { integrationsArea } from '@/features/integrations/content/integrations-nav';
import { webhookSettingsPage } from '@/features/integrations/content/webhook-settings';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: webhookSettingsPage.metaTitle };

/** Make an outbound webhook, and open one's delivery log. */
export default async function WebhooksPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.IntegrationManage)) {
    return <Alert tone="info">{integrationsArea.noAccess}</Alert>;
  }

  return (
    <div className="space-y-6">
      <PageHeader title={webhookSettingsPage.title} subtitle={webhookSettingsPage.subtitle} />
      <WebhookForm />
      <WebhookLogLookup />
    </div>
  );
}

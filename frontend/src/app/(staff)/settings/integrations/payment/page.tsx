import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { readPaymentSettings } from '@/features/integrations/api/providerSettings';
import { PaymentSettingsForm } from '@/features/integrations/components/payment-settings-form';
import { ProviderNotice } from '@/features/integrations/components/provider-notice';
import { integrationsArea } from '@/features/integrations/content/integrations-nav';
import { paymentSettingsPage } from '@/features/integrations/content/provider-settings';
import { authed } from '@/lib/auth/session';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: paymentSettingsPage.metaTitle };

/** The payment provider's settings. Stored only: there is no gateway connector yet. */
export default async function PaymentIntegrationPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.IntegrationManage)) {
    return <Alert tone="info">{integrationsArea.noAccess}</Alert>;
  }

  const settings = await authed(readPaymentSettings);

  return (
    <div className="space-y-6">
      <PageHeader title={paymentSettingsPage.title} subtitle={paymentSettingsPage.subtitle} />
      <ProviderNotice />
      <PaymentSettingsForm key={settings.version} settings={settings} />
    </div>
  );
}

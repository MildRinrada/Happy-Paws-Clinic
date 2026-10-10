import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getLoyaltySettings } from '@/features/crm/api/loyaltySettings';
import { LoyaltySettingsCard } from '@/features/crm/components/settings/loyalty-settings-card';
import { loyaltySettingsText } from '@/features/crm/content/loyalty-settings';
import { formMessages } from '@/features/settings/content/form-messages';
import { optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: loyaltySettingsText.metaTitle };

/**
 * The points programme (docs/contract-extensions.md § 25), on `settings:read` and `settings:write`
 * like the clinic's other rules. A session that cannot read the stored values is not offered a save.
 */
export default async function LoyaltySettingsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.SettingsWrite)) {
    return <Alert tone="info">{formMessages.noAccess}</Alert>;
  }

  const settings = await optional(getLoyaltySettings());

  return (
    <div className="max-w-3xl space-y-6">
      <PageHeader title={loyaltySettingsText.title} subtitle={loyaltySettingsText.subtitle} />
      <LoyaltySettingsCard settings={settings} />
    </div>
  );
}

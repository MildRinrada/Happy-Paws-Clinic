import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { AppointmentTypesCard } from '@/features/appointments/components/settings/appointment-types-card';
import { CancellationPolicyCard } from '@/features/appointments/components/settings/cancellation-policy-card';
import { appointmentSettingsText } from '@/features/appointments/content/appointment-settings';
import { formMessages } from '@/features/settings/content/form-messages';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: appointmentSettingsText.metaTitle };

/**
 * The cancellation policy and the appointment types. The contract can only write both, so nothing
 * is loaded: each form starts from the backend's defaults and says so.
 */
export default async function AppointmentSettingsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.SettingsWrite)) {
    return <Alert tone="info">{formMessages.noAccess}</Alert>;
  }

  return (
    <div className="space-y-6">
      <PageHeader title={appointmentSettingsText.title} subtitle={appointmentSettingsText.subtitle} />
      <CancellationPolicyCard />
      <AppointmentTypesCard />
    </div>
  );
}

import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getAppointmentTypes, getCancellationPolicy } from '@/features/appointments/api/appointmentSettings';
import { AppointmentTypesCard } from '@/features/appointments/components/settings/appointment-types-card';
import { CancellationPolicyCard } from '@/features/appointments/components/settings/cancellation-policy-card';
import { appointmentSettingsText } from '@/features/appointments/content/appointment-settings';
import { formMessages } from '@/features/settings/content/form-messages';
import { optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: appointmentSettingsText.metaTitle };

/**
 * The cancellation policy and the appointment types, each loaded as stored (docs/contract-extensions.md
 * § 23) and saved on its own. Each read has its own scope (`settings:read`, `appointment:read`);
 * a card whose read is refused says so instead of offering a replace it cannot show first.
 */
export default async function AppointmentSettingsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.SettingsWrite)) {
    return <Alert tone="info">{formMessages.noAccess}</Alert>;
  }

  const [policy, types] = await Promise.all([optional(getCancellationPolicy()), optional(getAppointmentTypes())]);

  return (
    <div className="space-y-6">
      <PageHeader title={appointmentSettingsText.title} subtitle={appointmentSettingsText.subtitle} />
      <CancellationPolicyCard policy={policy} />
      <AppointmentTypesCard types={types} />
    </div>
  );
}

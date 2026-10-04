import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getBookingSettings, readMedicalRecordSettings, readQueueSettings } from '@/features/clinic/api/clinic-rules';
import { BookingRulesForm } from '@/features/clinic/components/settings/booking-rules-form';
import { QueueRulesForm } from '@/features/clinic/components/settings/queue-rules-form';
import { RecordRulesForm } from '@/features/clinic/components/settings/record-rules-form';
import { rulesSettings } from '@/features/clinic/content/rules-settings';
import { formMessages } from '@/features/settings/content/form-messages';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: rulesSettings.metaTitle };

/** The clinic's three sets of operating rules, each saved on its own. */
export default async function RulesSettingsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.SettingsWrite)) {
    return <Alert tone="info">{formMessages.noAccess}</Alert>;
  }

  const [booking, queue, record] = await Promise.all([getBookingSettings(), readQueueSettings(), readMedicalRecordSettings()]);

  return (
    <div className="space-y-6">
      <PageHeader title={rulesSettings.title} subtitle={rulesSettings.subtitle} />
      <BookingRulesForm settings={booking} />
      <QueueRulesForm settings={queue} />
      <RecordRulesForm settings={record} />
    </div>
  );
}

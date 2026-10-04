import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getOpeningHours } from '@/features/clinic/api/getOpeningHours';
import { HolidaysCard } from '@/features/clinic/components/settings/holidays-card';
import { WeekHoursForm } from '@/features/clinic/components/settings/week-hours-form';
import { hoursSettings } from '@/features/clinic/content/hours-settings';
import { formMessages } from '@/features/settings/content/form-messages';
import { Scope } from '@/lib/auth/scopes';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: hoursSettings.metaTitle };

/** The week's hours as the website shows them, and the days the clinic is closed for booking. */
export default async function HoursSettingsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.SettingsWrite)) {
    return <Alert tone="info">{formMessages.noAccess}</Alert>;
  }

  const hours = await getOpeningHours();
  const branches = context.branches.map(({ id, name }) => ({ id, name }));

  return (
    <div className="space-y-6">
      <PageHeader title={hoursSettings.title} subtitle={hoursSettings.subtitle} />
      <WeekHoursForm hours={hours} />
      <HolidaysCard holidays={hours.holidays} branches={branches} today={clinicDay(new Date())} />
    </div>
  );
}

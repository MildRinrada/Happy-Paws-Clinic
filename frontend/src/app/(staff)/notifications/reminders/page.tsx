import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listUpcomingReminders } from '@/features/automation/api/listUpcomingReminders';
import { ReminderDaysFilter } from '@/features/automation/components/reminder-days-filter';
import { ReminderTable } from '@/features/automation/components/reminder-table';
import { remindersPage } from '@/features/automation/content/reminders-page';
import { authed } from '@/lib/auth/session';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: remindersPage.metaTitle };

interface RemindersPageProps {
  searchParams: Promise<{ days?: string }>;
}

/** Days the address may ask for; anything else falls back to the contract's seven. */
function readDays(value: string | undefined): number {
  const days = Number(value);
  return remindersPage.dayChoices.includes(days) ? days : 7;
}

/** "คิวข้อความที่จะถูกส่งใน 7 วันข้างหน้า": what the switched-on rules will send, at the branch in view. */
export default async function RemindersPage({ searchParams }: RemindersPageProps) {
  const context = await loadStaffContext();

  if (!context?.can(Scope.AutomationRead)) {
    return <Alert tone="info">{remindersPage.noAccess}</Alert>;
  }

  const days = readDays((await searchParams).days);
  const upcoming = await authed(() => listUpcomingReminders(days, context.branch?.id));

  return (
    <div className="space-y-5">
      <PageHeader title={remindersPage.title} subtitle={remindersPage.subtitle(days)} actions={<ReminderDaysFilter days={days} />} />

      {upcoming.data.length === 0 ? <EmptyState title={remindersPage.empty} /> : <ReminderTable reminders={upcoming.data} showRule />}
      {upcoming.truncated && <p className="text-xs text-ink-500">{remindersPage.truncated}</p>}
    </div>
  );
}

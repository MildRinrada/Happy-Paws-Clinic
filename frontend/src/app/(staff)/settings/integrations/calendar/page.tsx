import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getCalendarLink } from '@/features/integrations/api/calendarLink';
import { CalendarLinkCard } from '@/features/integrations/components/calendar-link-card';
import { RotateCalendarLinkButton } from '@/features/integrations/components/rotate-calendar-link-button';
import { calendarFeedPage } from '@/features/integrations/content/calendar-feed';
import { feedUrl } from '@/features/integrations/lib/feed-url';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: calendarFeedPage.metaTitle };

/** The signed-in staff member's calendar subscription link. Any staff member may open this. */
export default async function CalendarFeedPage() {
  const context = await loadStaffContext();

  if (!context) {
    return <Alert tone="info">{calendarFeedPage.unavailable}</Alert>;
  }

  const link = await authed(getCalendarLink);

  return (
    <div className="space-y-6">
      <PageHeader title={calendarFeedPage.title} subtitle={calendarFeedPage.subtitle} />
      <CalendarLinkCard mineUrl={feedUrl(link.path)} branchUrl={link.can_view_branch ? feedUrl(link.path, 'branch') : undefined} />
      <RotateCalendarLinkButton />
    </div>
  );
}

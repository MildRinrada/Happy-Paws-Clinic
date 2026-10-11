import type { Metadata } from 'next';
import { HomeGreeting } from '@/features/portal/components/home/home-greeting';
import { LatestNotifications } from '@/features/portal/components/home/latest-notifications';
import { PetsOverview } from '@/features/portal/components/home/pets-overview';
import { QuickActions } from '@/features/portal/components/home/quick-actions';
import { VaccinesDue } from '@/features/portal/components/home/vaccines-due';
import { LiveRefresh } from '@/features/portal/components/visit-card/live-refresh';
import { VisitCard } from '@/features/portal/components/visit-card/visit-card';
import { VisitEmpty } from '@/features/portal/components/visit-card/visit-empty';
import { portalHome } from '@/features/portal/content/portal-home';
import { loadPortalHome } from '@/features/portal/lib/load-portal-home';
import { loadVisitCards } from '@/features/portal/lib/load-visit-card';
import { PendingReviewsCard } from '@/features/reviews/components/portal/pending-reviews-card';

export const metadata: Metadata = { title: `${portalHome.metaTitle} · Happy Paws Clinic` };

/** How often the page is re-read while a visit is happening today, in seconds. */
const REFRESH_SECONDS = 15;

/**
 * The owner's home in the portal, most urgent first: today's visit and queue, the shortcuts,
 * vaccines falling due, a visit waiting for a review, their pets, and the latest messages.
 *
 * On a phone it is one column in that order. From 1024px the visit and the shortcuts stand on the
 * left and the rest on the right, which keeps the same reading order.
 */
export default async function PortalHomePage() {
  const [home, visits] = await Promise.all([loadPortalHome(), loadVisitCards()]);

  return (
    // Plex for reading, Prompt (`font-display`) for headings, numbers and buttons, as the design sets them.
    <div className="space-y-5 font-sans">
      <HomeGreeting name={home.display_name} />

      <div className="grid items-start gap-5 lg:grid-cols-[420px_minmax(0,1fr)] lg:gap-6">
        <div className="space-y-5">
          {/* One card per pet being seen today, so each queue number sits with its own pet. */}
          {visits.length > 0 ? (
            visits.map((visit) => (
              <VisitCard key={visit.appointment?.id ?? visit.ticket?.ticket_id} view={visit} />
            ))
          ) : (
            <VisitEmpty />
          )}
          <QuickActions />
        </div>

        <div className="space-y-5">
          <VaccinesDue items={home.vaccinations_due} />
          <PendingReviewsCard />
          <PetsOverview pets={home.pets} />
          <LatestNotifications items={home.latest_notifications} />
        </div>
      </div>

      {visits.some((visit) => visit.today) && <LiveRefresh seconds={REFRESH_SECONDS} />}
    </div>
  );
}

import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';
import { loadPageBanner } from '@/features/site-banners/lib/load-page-banner';
import { TeamBanner } from '@/features/team/components/team-banner';
import { TeamDirectory } from '@/features/team/components/team-directory';
import { teamPage } from '@/features/team/content/team-page';
import { loadTeam } from '@/features/team/lib/load-team';

export const metadata = pageMetadata({
  title: `${teamPage.title} | Happy Paws Clinic`,
  description: teamPage.intro,
  path: sitePages.team.path,
});

/**
 * Everyone the clinic lists on its website (`GET /team`), grouped by department: a banner from
 * `loadPageBanner('team')`, a bar of links to each department, then a portrait card per person.
 * Each vet's card links to booking with that vet already chosen.
 */
export default async function TeamPage() {
  const [banner, departments] = await Promise.all([loadPageBanner('team'), loadTeam()]);

  return (
    <main>
      <TeamBanner banner={banner} />
      <TeamDirectory departments={departments} />

      <BreadcrumbJsonLd trail={[sitePages.team]} />
    </main>
  );
}

import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/features/home/components/shared/section-heading';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';
import { TeamDirectory } from '@/features/team/components/team-directory';
import { teamPage } from '@/features/team/content/team-page';
import { loadTeam } from '@/features/team/lib/load-team';

export const metadata = pageMetadata({
  title: `${teamPage.title} | Happy Paws Clinic`,
  description: teamPage.intro,
  path: sitePages.team.path,
});

/** Everyone the clinic lists on its website, grouped by department. */
export default async function TeamPage() {
  const departments = await loadTeam();

  return (
    <main className="py-12">
      <SiteContainer>
        <SectionHeading id="team-heading">{teamPage.title}</SectionHeading>
        <p className="mt-2 text-[15px] text-site-muted">{teamPage.intro}</p>

        <TeamDirectory departments={departments} />
      </SiteContainer>

      <BreadcrumbJsonLd trail={[sitePages.team]} />
    </main>
  );
}

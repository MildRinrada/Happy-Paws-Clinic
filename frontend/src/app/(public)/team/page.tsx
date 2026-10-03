import type { Metadata } from 'next';
import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/features/home/components/shared/section-heading';
import { TeamDirectory } from '@/features/team/components/team-directory';
import { teamPage } from '@/features/team/content/team-page';
import { loadTeam } from '@/features/team/lib/load-team';

export const metadata: Metadata = {
  title: `${teamPage.title} | Happy Paws Clinic`,
};

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
    </main>
  );
}

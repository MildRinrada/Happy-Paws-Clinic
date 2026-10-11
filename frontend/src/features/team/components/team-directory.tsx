import { SiteContainer } from '@/components/layout/site-container';
import { teamPage } from '../content/team-page';
import type { TeamDepartment } from '../types/team.types';
import { DepartmentNav } from './department-nav';
import { DepartmentSection } from './department-section';

/**
 * The whole team under the banner: a bar of links to each department, then one section per
 * department.
 *
 * `null` means the team could not be read (the API is down), which is said differently from a
 * clinic that has not put anyone on the website yet. Either way there is no bar.
 */
export function TeamDirectory({ departments }: { departments: TeamDepartment[] | null }) {
  if (departments === null || departments.length === 0) {
    return (
      <SiteContainer className="pb-16 pt-10">
        <p className="text-[15px] text-site-muted">{departments === null ? teamPage.unavailable : teamPage.empty}</p>
      </SiteContainer>
    );
  }

  return (
    <>
      <DepartmentNav departments={departments} />

      <SiteContainer className="space-y-14 pb-16 pt-12">
        {departments.map((department) => (
          <DepartmentSection key={department.department} department={department} />
        ))}
      </SiteContainer>
    </>
  );
}

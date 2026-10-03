import { teamPage } from '../content/team-page';
import type { TeamDepartment } from '../types/team.types';
import { DepartmentNav } from './department-nav';
import { DepartmentSection } from './department-section';

/**
 * The whole team, one section per department, with a row of links to each.
 *
 * `null` means the team could not be read (the API is down), which is said differently from a
 * clinic that has not put anyone on the website yet.
 */
export function TeamDirectory({ departments }: { departments: TeamDepartment[] | null }) {
  if (departments === null) {
    return <p className="mt-7 text-[15px] text-site-muted">{teamPage.unavailable}</p>;
  }

  if (departments.length === 0) {
    return <p className="mt-7 text-[15px] text-site-muted">{teamPage.empty}</p>;
  }

  return (
    <>
      <DepartmentNav departments={departments} />

      <div className="mt-10 space-y-12">
        {departments.map((department) => (
          <DepartmentSection key={department.department} department={department} />
        ))}
      </div>
    </>
  );
}

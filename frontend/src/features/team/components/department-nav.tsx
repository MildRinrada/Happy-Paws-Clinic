import { ChipNav } from '@/components/site/chip-nav';
import { DotChip } from '@/components/site/dot-chip';
import { teamPage } from '../content/team-page';
import type { TeamDepartment } from '../types/team.types';

/** The bar under the banner: one chip per department with someone listed, each jumping down to it. */
export function DepartmentNav({ departments }: { departments: TeamDepartment[] }) {
  return (
    <ChipNav label={teamPage.navLabel}>
      {departments.map((department) => (
        <DotChip key={department.department} href={`#${department.department}`} label={department.name} />
      ))}
    </ChipNav>
  );
}

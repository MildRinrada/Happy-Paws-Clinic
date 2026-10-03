import { teamPage } from '../content/team-page';
import type { TeamDepartment } from '../types/team.types';

/** A row of links, one per department, that jump down the page. */
export function DepartmentNav({ departments }: { departments: TeamDepartment[] }) {
  return (
    <nav aria-label={teamPage.navLabel} className="mt-6">
      <ul className="flex flex-wrap gap-2">
        {departments.map((department) => (
          <li key={department.department}>
            <a
              href={`#${department.department}`}
              className="flex h-9 items-center gap-1.5 rounded-full border border-site-border bg-white px-4 text-sm hover:border-site-teal hover:text-site-teal"
            >
              {department.name}
              <span className="text-site-muted">{department.members.length}</span>
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}

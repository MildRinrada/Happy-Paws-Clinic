import { departmentBlurbs } from '../content/team-page';
import type { TeamDepartment } from '../types/team.types';
import { TeamMemberCard } from './team-member-card';

/** One department: its name, how many people, one line on what it does, and a card per person. */
export function DepartmentSection({ department }: { department: TeamDepartment }) {
  const headingId = `${department.department}-heading`;
  const blurb = departmentBlurbs[department.department];

  return (
    <section id={department.department} aria-labelledby={headingId} className="scroll-mt-6">
      <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
        <h2 id={headingId} className="text-xl font-semibold sm:text-[22px]">
          {department.name}
        </h2>
        <span className="text-sm text-site-muted">{department.members.length} คน</span>
      </div>
      {blurb && <p className="mt-1.5 text-[15px] text-site-muted">{blurb}</p>}

      <ul className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {department.members.map((member) => (
          <TeamMemberCard key={member.id} member={member} />
        ))}
      </ul>
    </section>
  );
}

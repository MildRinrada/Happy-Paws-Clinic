import { SectionHeader } from '@/components/site/section-header';
import { departmentBlurbs } from '../content/team-page';
import type { TeamDepartment } from '../types/team.types';
import { TeamMemberCard } from './team-member-card';

/**
 * One department: its name, one line on what it does, a dark rule, and a portrait card per person.
 * Vets' cards are drawn in pale green and link to booking with that vet.
 */
export function DepartmentSection({ department }: { department: TeamDepartment }) {
  const headingId = `${department.department}-heading`;
  const isVets = department.department === 'veterinary';

  return (
    <section id={department.department} aria-labelledby={headingId} className="scroll-mt-6">
      <SectionHeader id={headingId} title={department.name} blurb={departmentBlurbs[department.department]} />

      <ul className="mt-5 grid grid-cols-2 gap-x-4 gap-y-9 md:grid-cols-3 lg:grid-cols-4 lg:gap-x-5">
        {department.members.map((member) => (
          <TeamMemberCard key={member.id} member={member} vet={isVets} />
        ))}
      </ul>
    </section>
  );
}

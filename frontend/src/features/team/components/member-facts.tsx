import { MetaPill } from '@/components/site/meta-pill';
import { teamPage } from '../content/team-page';
import type { TeamMember } from '../types/team.types';

/** A person's field in a small pill, and how long they have done this work. Nothing when neither is known. */
export function MemberFacts({ member }: { member: Pick<TeamMember, 'specialty' | 'years_of_experience'> }) {
  const { specialty, years_of_experience: years } = member;

  if (!specialty && years === undefined) return null;

  return (
    <div className="mt-2.5 flex flex-wrap items-center gap-x-2 gap-y-1.5">
      {specialty && (
        <MetaPill tone="softForest" className="max-w-full">
          <span className="truncate" title={specialty}>
            {specialty}
          </span>
        </MetaPill>
      )}
      {years !== undefined && <span className="text-xs text-site-muted">{teamPage.experience(years)}</span>}
    </div>
  );
}

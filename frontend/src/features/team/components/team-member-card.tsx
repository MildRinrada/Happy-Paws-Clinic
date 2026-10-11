import { teamPage } from '../content/team-page';
import type { TeamMember } from '../types/team.types';
import { BookVetLink } from './book-vet-link';
import { MemberFacts } from './member-facts';
import { MemberPhoto } from './member-photo';

interface Props {
  member: TeamMember;
  /** A vet's card: a pale green frame while there is no photo, and a link to book with them. */
  vet: boolean;
}

/**
 * One person on the team page: a tall photo, name, title, field and years, a few words from them,
 * and for a vet a link to book with them. A column, so the vets' links line up along a row.
 */
export function TeamMemberCard({ member, vet }: Props) {
  return (
    <li id={member.id} className="flex min-w-0 scroll-mt-6 flex-col">
      <MemberPhoto
        member={member}
        label={teamPage.photoLabel}
        tone={vet ? 'mint' : 'soft'}
        sizes="(min-width: 1024px) 240px, (min-width: 768px) 33vw, 50vw"
        className="aspect-[4/5] w-full overflow-hidden rounded-[20px]"
      />

      <h3 className="mt-3.5 text-[17px] font-bold leading-snug text-ink-900">{member.display_name}</h3>
      {member.job_title && <p className="mt-1 text-sm text-ink-600">{member.job_title}</p>}
      <MemberFacts member={member} />
      {member.bio && <p className="mt-2.5 text-[13px] leading-5 text-ink-700">{member.bio}</p>}
      {vet && <BookVetLink member={member} />}
    </li>
  );
}

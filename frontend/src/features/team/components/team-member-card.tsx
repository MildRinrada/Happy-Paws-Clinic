import { teamPage } from '../content/team-page';
import { memberSummary } from '../lib/team-text';
import type { TeamMember } from '../types/team.types';
import { MemberPhoto } from './member-photo';

/** One person on the team page: photo, name, title, field and years, and a few words from them. */
export function TeamMemberCard({ member }: { member: TeamMember }) {
  const summary = memberSummary(member);

  return (
    <li id={member.id} className="scroll-mt-6 overflow-hidden rounded-2xl border border-site-border bg-white">
      <MemberPhoto
        member={member}
        label={teamPage.photoLabel}
        sizes="(min-width: 1024px) 22vw, (min-width: 640px) 50vw, 100vw"
        className="h-[196px] border-x-0 border-t-0"
      />

      <div className="px-5 pb-6 pt-5">
        <h3 className="font-bold">{member.display_name}</h3>
        {member.job_title && <p className="mt-1.5 text-[13px] font-bold text-site-teal">{member.job_title}</p>}
        {summary && <p className="mt-1.5 text-[13px] text-site-muted">{summary}</p>}
        {member.bio && <p className="mt-3 text-sm leading-relaxed">{member.bio}</p>}
      </div>
    </li>
  );
}

import Link from 'next/link';
import { bookVetLink } from '../content/team-page';
import type { TeamMember } from '../types/team.types';

/**
 * "จองนัดกับหมอท่านนี้": opens booking with this vet already chosen (`/book?vet=`). Sits at the
 * card's foot, so the links line up along a row whatever is written above them.
 */
export function BookVetLink({ member }: { member: Pick<TeamMember, 'id' | 'display_name'> }) {
  return (
    <Link
      href={bookVetLink.href(member.id)}
      className="mt-auto self-start pt-3 text-[15px] font-bold text-site-forest underline underline-offset-4 hover:text-site-teal"
    >
      {bookVetLink.label}
      <span className="sr-only"> {member.display_name}</span>
    </Link>
  );
}

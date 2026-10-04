import { Card, CardHeader } from '@/components/ui/card';
import { staffSettings } from '../../content/staff-settings';
import type { StaffDirectory } from '../../types/staff-settings.types';
import type { StaffListFilters, StaffMember } from '../../types/staff.types';
import { StaffMemberRow } from './staff-member-row';
import { StaffPager } from './staff-pager';

interface StaffListCardProps {
  members: StaffMember[];
  filters: StaffListFilters;
  nextCursor?: string;
  /** True when open invitations were put at the top of the "all" list. */
  invitedFirst: boolean;
  directory: StaffDirectory;
}

/** The list itself: one row per person, then the way to the next page. */
export function StaffListCard({ members, filters, nextCursor, invitedFirst, directory }: StaffListCardProps) {
  const text = staffSettings.list;

  return (
    <Card>
      <CardHeader title={text.title} aside={<span className="text-sm text-ink-500">{text.count(members.length)}</span>} />
      {invitedFirst && <p className="border-b border-ink-100 px-5 py-2 text-xs text-ink-500">{text.invitedFirst}</p>}
      {members.length === 0 ? (
        <p className="px-5 py-4 text-sm text-ink-500">{text.empty}</p>
      ) : (
        <ul className="divide-y divide-ink-100">
          {members.map((member) => (
            // Keyed by id, not version: a re-sent invitation's notice must survive the refresh it causes.
            <StaffMemberRow key={member.id} member={member} directory={directory} />
          ))}
        </ul>
      )}
      <StaffPager filters={filters} nextCursor={nextCursor} />
    </Card>
  );
}

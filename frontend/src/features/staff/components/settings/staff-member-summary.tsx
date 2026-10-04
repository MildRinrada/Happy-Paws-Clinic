import { Badge, type BadgeTone } from '@/components/ui/badge';
import { formatDateTime } from '@/lib/format/datetime';
import { roleLabel, staffTypeLabel, statusLabels } from '../../content/staff-labels';
import { staffSettings } from '../../content/staff-settings';
import type { StaffDirectory } from '../../types/staff-settings.types';
import type { StaffMember, StaffStatus } from '../../types/staff.types';

const statusTones: Record<StaffStatus, BadgeTone> = { active: 'green', invited: 'amber', deactivated: 'red' };

interface StaffMemberSummaryProps {
  member: StaffMember;
  directory: Pick<StaffDirectory, 'departments' | 'branches' | 'selfId'>;
}

/** Who someone is and what they hold: position, team, roles, branches, status and 2FA. */
export function StaffMemberSummary({ member, directory }: StaffMemberSummaryProps) {
  const text = staffSettings.member;
  const department = directory.departments.find((entry) => entry.department === member.department)?.name ?? member.department;
  const branches =
    member.branches.length === 0 ? text.allBranches : member.branches.map((id) => directory.branches[id] ?? id).join(', ');
  const details = [staffTypeLabel(member.staff_type), department, member.license_no && text.license(member.license_no)].filter(Boolean);

  return (
    <div className="min-w-0 flex-1 space-y-1">
      <p className="flex flex-wrap items-center gap-2">
        <span className={`font-medium ${member.status === 'deactivated' ? 'text-ink-400' : 'text-ink-900'}`}>{member.display_name}</span>
        <Badge tone={statusTones[member.status]}>{statusLabels[member.status]}</Badge>
        {member.status !== 'invited' && (
          <Badge tone={member.two_factor_enabled ? 'blue' : 'neutral'}>{member.two_factor_enabled ? text.twoFactorOn : text.twoFactorOff}</Badge>
        )}
        {member.id === directory.selfId && <Badge tone="brand">{text.self}</Badge>}
      </p>
      <p className="break-all text-sm text-ink-600">{member.email ?? text.noEmail}</p>
      <p className="text-sm text-ink-600">{details.join(' · ')}</p>
      <p className="flex flex-wrap items-center gap-1.5 text-sm text-ink-600">
        {member.roles.length === 0 ? (
          <Badge>{text.noRoles}</Badge>
        ) : (
          member.roles.map((role) => (
            <Badge key={role} tone="brand">
              {roleLabel(role)}
            </Badge>
          ))
        )}
        <span className="ml-1">{branches}</span>
      </p>
      {member.invitation_expires_at && <p className="text-xs text-amber-800">{text.expires(formatDateTime(member.invitation_expires_at))}</p>}
    </div>
  );
}

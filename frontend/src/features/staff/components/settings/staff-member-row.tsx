'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { staffSettings } from '../../content/staff-settings';
import { canChangeHolder } from '../../lib/scope-grants';
import type { StaffDirectory } from '../../types/staff-settings.types';
import type { StaffMember } from '../../types/staff.types';
import { DeactivateConfirm } from './deactivate-confirm';
import { ResendInvitationButton } from './resend-invitation-button';
import { StaffMemberForm } from './staff-member-form';
import { StaffMemberSummary } from './staff-member-summary';

type Mode = 'view' | 'edit' | 'deactivate';

/**
 * One person and what can be done about them, which depends on their status:
 * - active: edit and deactivate, unless they hold more than the caller (the backend would refuse);
 * - invited: send again, the only change the API allows before acceptance;
 * - deactivated: nothing, as there is no endpoint to bring someone back.
 */
export function StaffMemberRow({ member, directory }: { member: StaffMember; directory: StaffDirectory }) {
  const text = staffSettings.actions;
  const [mode, setMode] = useState<Mode>('view');
  const close = () => setMode('view');

  const manageable = directory.canManage && canChangeHolder(directory.callerScopes, member.roles, directory.roles);
  const isSelf = member.id === directory.selfId;

  return (
    <li className="space-y-3 px-5 py-4">
      <div className="flex flex-wrap items-start gap-3">
        <StaffMemberSummary member={member} directory={directory} />

        {directory.canManage && member.status === 'invited' && manageable && <ResendInvitationButton member={member} />}

        {member.status === 'active' && manageable && (
          <div className="flex flex-wrap gap-2">
            <Button variant={mode === 'edit' ? 'primary' : 'secondary'} size="sm" onClick={() => setMode(mode === 'edit' ? 'view' : 'edit')}>
              {text.edit}
            </Button>
            {!isSelf && (
              <Button variant="danger" size="sm" onClick={() => setMode('deactivate')}>
                {text.deactivate}
              </Button>
            )}
          </div>
        )}
      </div>

      {directory.canManage && member.status === 'active' && !manageable && <p className="text-xs text-ink-500">{staffSettings.member.outranks}</p>}

      {/* Rows are keyed by id, so after a deactivation the row stays mounted; the status hides the panel. */}
      {member.status === 'active' && mode === 'edit' && <StaffMemberForm member={member} directory={directory} onDone={close} />}
      {member.status === 'active' && mode === 'deactivate' && <DeactivateConfirm member={member} onCancel={close} />}
    </li>
  );
}

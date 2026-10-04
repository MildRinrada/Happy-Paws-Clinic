'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import { inviteStaffAction } from '../../actions/staff-actions';
import { staffSettings } from '../../content/staff-settings';
import type { StaffMember } from '../../types/staff.types';
import { InvitationSentNotice } from './invitation-sent-notice';

/**
 * Sends a pending invitation again, as it stands. There is no re-send endpoint: inviting the same
 * address re-issues the open invitation, kills the old link and restarts the seven days.
 */
export function ResendInvitationButton({ member }: { member: StaffMember }) {
  const [sent, setSent] = useState<{ email: string; expiresAt?: string }>();
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function resend() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await inviteStaffAction({
          email: member.email ?? '',
          display_name: member.display_name,
          staff_type: member.staff_type,
          roles: member.roles,
          branches: member.branches,
          license_no: member.license_no,
        });
        if (result.ok) setSent(result.data);
        else setError(result.error.message);
      } catch {
        setError(formMessages.unreachable);
      }
    });
  }

  if (sent) {
    return <InvitationSentNotice email={sent.email} expiresAt={sent.expiresAt} onDismiss={() => setSent(undefined)} />;
  }

  return (
    <div className="space-y-1">
      <Button variant="secondary" size="sm" disabled={pending || !member.email} onClick={resend}>
        {pending ? staffSettings.actions.resending : staffSettings.actions.resend}
      </Button>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}

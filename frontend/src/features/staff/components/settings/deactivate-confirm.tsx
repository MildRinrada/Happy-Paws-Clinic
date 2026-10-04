'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import { deactivateStaffAction } from '../../actions/staff-actions';
import { staffSettings } from '../../content/staff-settings';
import type { StaffMember } from '../../types/staff.types';

interface DeactivateConfirmProps {
  member: StaffMember;
  onCancel: () => void;
}

/**
 * The second press before someone is locked out. A refusal is the backend's own Thai sentence:
 * `last_owner` when this is the only account holding `*`, `cannot_deactivate_self` for yourself.
 */
export function DeactivateConfirm({ member, onCancel }: DeactivateConfirmProps) {
  const text = staffSettings.deactivate;
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function confirm() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await deactivateStaffAction({ id: member.id });
        if (!result.ok) setError(result.error.message);
      } catch {
        setError(formMessages.unreachable);
      }
    });
  }

  return (
    <div className="space-y-3 rounded-xl border border-red-200 bg-red-50/60 p-4">
      <p className="text-sm text-red-900">{text.confirm(member.display_name)}</p>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
      <div className="flex flex-wrap justify-end gap-2">
        <Button variant="ghost" size="sm" disabled={pending} onClick={onCancel}>
          {text.cancel}
        </Button>
        <Button variant="danger" size="sm" disabled={pending} onClick={confirm}>
          {pending ? text.pending : text.yes}
        </Button>
      </div>
    </div>
  );
}

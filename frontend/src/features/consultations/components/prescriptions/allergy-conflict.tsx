'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { consultationPage } from '../../content/consultation-page';

export interface ConflictInfo {
  message: string;
  overridable: boolean;
}

interface AllergyConflictProps {
  conflict: ConflictInfo;
  pending: boolean;
  onConfirm: (reason: string) => void;
}

/**
 * The stop a `409 allergy_conflict` asks for. A severe allergy ends here; a lesser one can still be
 * prescribed, with the vet's reason written onto the prescription and into the audit log.
 */
export function AllergyConflict({ conflict, pending, onConfirm }: AllergyConflictProps) {
  const text = consultationPage.prescriptions.conflict;
  const [reason, setReason] = useState('');

  return (
    <div role="alert" className="space-y-3 rounded-lg border border-red-300 bg-red-50 p-4">
      <div>
        <p className="font-semibold text-red-800">{text.title}</p>
        <p className="text-sm text-red-800">{conflict.message}</p>
      </div>

      {conflict.overridable ? (
        <div className="flex flex-wrap items-end gap-3">
          <div className="min-w-0 flex-1">
            <Field label={text.reason} htmlFor="allergy-acknowledge" hint={text.reasonHint}>
              <Input id="allergy-acknowledge" value={reason} maxLength={500} onChange={(event) => setReason(event.target.value)} />
            </Field>
          </div>
          <Button variant="danger" disabled={pending || reason.trim() === ''} onClick={() => onConfirm(reason.trim())}>
            {text.confirm}
          </Button>
        </div>
      ) : (
        <p className="text-sm font-medium text-red-900">{text.severe}</p>
      )}
    </div>
  );
}

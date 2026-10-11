'use client';

import { useId, useState, type FormEvent } from 'react';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateBranchAction } from '../../actions/clinic-setup-actions';
import { clinicSettings } from '../../content/clinic-settings';
import { branchDraft, branchUpdate, type BranchDraft } from '../../lib/branch-draft';
import type { Branch } from '../../types/clinic.types';
import { BranchFields } from './branch-fields';

/**
 * Changes a branch (docs/contract-extensions.md § 51), inside its "แก้ไข" dialog. The code is shown
 * but fixed. A room that a ticket on the queue is still in is refused by the backend, which names
 * the tickets; the message shows under the rooms.
 */
export function EditBranchForm({ branch, onDone }: { branch: Branch; onDone: () => void }) {
  const text = clinicSettings.branches;
  const idPrefix = useId();
  const [values, setValues] = useState<BranchDraft>(() => branchDraft(branch));
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  const set = (field: keyof BranchDraft, value: string) => setValues((current) => ({ ...current, [field]: value }));

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => updateBranchAction({ id: branch.id, version: branch.version, values: branchUpdate(values) }), onDone);
  }

  return (
    <form onSubmit={save} className="space-y-4">
      <div className="space-y-1">
        <p className="text-sm font-medium text-ink-700">{text.code}</p>
        <p className="font-mono text-sm text-ink-900">{branch.code}</p>
        <p className="text-sm text-ink-500">{text.codeFixed}</p>
      </div>

      <BranchFields
        idPrefix={idPrefix}
        values={values}
        errors={errors}
        onChange={set}
        coordinatesHint={text.coordinatesKeptHint}
        roomsHint={text.examRoomsEditHint}
      />

      <FormFooter pending={pending} message={message} onCancel={onDone} />
    </form>
  );
}

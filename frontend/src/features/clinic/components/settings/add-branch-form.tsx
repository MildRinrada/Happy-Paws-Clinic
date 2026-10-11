'use client';

import { useId, useState, type FormEvent } from 'react';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createBranchAction } from '../../actions/clinic-setup-actions';
import { clinicSettings } from '../../content/clinic-settings';
import { blankBranchDraft, type BranchDraft } from '../../lib/branch-draft';
import { optionalNumber, parseCommaList } from '../../lib/parse-list';
import { BranchFields } from './branch-fields';

/** Opens a new branch, inside the "เปิดสาขาใหม่" dialog. `onDone` closes it once the branch is in. */
export function AddBranchForm({ onDone }: { onDone: () => void }) {
  const text = clinicSettings.branches;
  const idPrefix = useId();
  const [code, setCode] = useState('');
  const [values, setValues] = useState<BranchDraft>(blankBranchDraft);
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  const set = (field: keyof BranchDraft, value: string) => setValues((current) => ({ ...current, [field]: value }));

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    submit(
      () =>
        createBranchAction({
          code: code.trim().toLowerCase(),
          name: values.name,
          address: values.address.trim() || undefined,
          latitude: optionalNumber(values.latitude),
          longitude: optionalNumber(values.longitude),
          exam_rooms: parseCommaList(values.rooms),
        }),
      onDone,
    );
  }

  return (
    <form onSubmit={save} className="space-y-4">
      <Field label={text.code} htmlFor={`${idPrefix}-code`} hint={text.codeHint} error={errors.code}>
        <Input id={`${idPrefix}-code`} value={code} maxLength={32} required className="font-mono" onChange={(event) => setCode(event.target.value)} />
      </Field>

      <BranchFields
        idPrefix={idPrefix}
        values={values}
        errors={errors}
        onChange={set}
        coordinatesHint={text.coordinatesHint}
        roomsHint={text.examRoomsHint}
      />

      <FormFooter pending={pending} message={message} onCancel={onDone} submitLabel={formMessages.add} pendingLabel={formMessages.adding} />
    </form>
  );
}

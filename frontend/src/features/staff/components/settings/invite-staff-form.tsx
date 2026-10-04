'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { inviteStaffAction } from '../../actions/staff-actions';
import { staffSettings } from '../../content/staff-settings';
import type { StaffDirectory } from '../../types/staff-settings.types';
import { InvitationSentNotice } from './invitation-sent-notice';
import { RolePicker } from './role-picker';
import { StaffTypeInput } from './staff-type-input';

const blank = { email: '', display_name: '', staff_type: '', license_no: '', roles: [] as string[], branches: [] as string[] };

type Draft = typeof blank;

/**
 * A new person: who, as what, with which roles at which branches. Nothing they can use exists until
 * they accept; what is saved is the offer and a link that lasts seven days.
 */
export function InviteStaffForm({ directory }: { directory: StaffDirectory }) {
  const text = staffSettings.invite;
  const branchIds = Object.keys(directory.branches);
  // A caller bound to a single branch can only invite to it, so it starts ticked.
  const start: Draft = { ...blank, branches: directory.branchBound && branchIds.length === 1 ? branchIds : [] };
  const [open, setOpen] = useState(false);
  const [values, setValues] = useState<Draft>(start);
  const [sent, setSent] = useState<{ email: string; expiresAt?: string }>();
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  const set = <K extends keyof Draft>(field: K, value: Draft[K]) => setValues((current) => ({ ...current, [field]: value }));

  if (!open) {
    return (
      <div className="space-y-3">
        {sent && <InvitationSentNotice email={sent.email} expiresAt={sent.expiresAt} onDismiss={() => setSent(undefined)} />}
        <Button onClick={() => setOpen(true)}>{text.open}</Button>
      </div>
    );
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () =>
        inviteStaffAction({
          email: values.email.trim(),
          display_name: values.display_name.trim(),
          staff_type: values.staff_type.trim(),
          roles: values.roles,
          branches: values.branches,
          license_no: values.license_no.trim() || undefined,
        }),
      (data) => {
        setSent(data);
        setValues(start);
        setOpen(false);
      },
    );
  }

  return (
    <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-white p-4">
      <div className="space-y-1">
        <h2 className="font-display text-base font-semibold text-ink-900">{text.title}</h2>
        <p className="text-sm text-ink-500">{text.intro}</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.email} htmlFor="invite-email" error={errors.email}>
          <Input id="invite-email" type="email" value={values.email} maxLength={256} required autoComplete="off" onChange={(event) => set('email', event.target.value)} />
        </Field>

        <Field label={text.displayName} htmlFor="invite-name" hint={text.displayNameHint} error={errors.display_name}>
          <Input id="invite-name" value={values.display_name} maxLength={200} required onChange={(event) => set('display_name', event.target.value)} />
        </Field>

        <StaffTypeInput
          id="invite-type"
          value={values.staff_type}
          options={directory.staffTypes}
          error={errors.staff_type}
          onChange={(value) => set('staff_type', value)}
        />

        <Field label={staffSettings.edit.license} htmlFor="invite-license" hint={staffSettings.edit.licenseHint} error={errors.license_no}>
          <Input id="invite-license" value={values.license_no} maxLength={64} onChange={(event) => set('license_no', event.target.value)} />
        </Field>
      </div>

      <p className="text-sm text-ink-500">{text.departmentNote}</p>

      {directory.roles && (
        <RolePicker
          roles={directory.roles}
          callerScopes={directory.callerScopes}
          selected={values.roles}
          error={errors.roles}
          onChange={(roles) => set('roles', roles)}
        />
      )}

      {(branchIds.length > 1 || directory.branchBound) && (
        <CheckboxGroup
          legend={staffSettings.edit.branches}
          hint={directory.branchBound ? undefined : staffSettings.edit.branchesHint}
          error={errors.branches}
          options={directory.branches}
          selected={values.branches}
          onChange={(branches) => set('branches', branches)}
        />
      )}

      <FormFooter pending={pending} message={message} onCancel={() => setOpen(false)} submitLabel={text.submit} pendingLabel={text.pending} />
    </form>
  );
}

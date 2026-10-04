'use client';

import { useState, type FormEvent } from 'react';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Input, Select } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateStaffAction } from '../../actions/staff-actions';
import { staffSettings } from '../../content/staff-settings';
import { draftOf, memberChanges, type MemberDraft } from '../../lib/member-changes';
import type { StaffDirectory } from '../../types/staff-settings.types';
import type { StaffMember } from '../../types/staff.types';
import { RolePicker } from './role-picker';
import { StaffTypeInput } from './staff-type-input';

interface StaffMemberFormProps {
  member: StaffMember;
  directory: StaffDirectory;
  onDone: () => void;
}

/** Position, team, licence, roles and branches. Only the fields that changed are sent. */
export function StaffMemberForm({ member, directory, onDone }: StaffMemberFormProps) {
  const text = staffSettings.edit;
  const [draft, setDraft] = useState<MemberDraft>(() => draftOf(member));
  const { errors, message, pending, submit } = useFormSubmit(formMessages);
  const id = (field: string) => `staff-${member.id}-${field}`;

  // A branch this caller cannot see stays on the list under its id, so saving does not drop it.
  const branchOptions = { ...directory.branches };
  for (const branchId of member.branches) branchOptions[branchId] ??= text.unknownBranch(branchId);

  const set = <K extends keyof MemberDraft>(field: K, value: MemberDraft[K]) => setDraft((current) => ({ ...current, [field]: value }));

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    // Nothing to send is not a save: close the form rather than bump the version for nothing.
    const values = memberChanges(member, draft);
    if (Object.keys(values).length === 0) return onDone();

    submit(() => updateStaffAction({ id: member.id, version: member.version, values }), onDone);
  }

  return (
    <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
      <p className="text-sm text-ink-500">{text.noDisplayName}</p>

      <div className="grid gap-4 sm:grid-cols-3">
        <StaffTypeInput
          id={id('type')}
          value={draft.staff_type}
          options={directory.staffTypes}
          error={errors.staff_type}
          onChange={(value) => set('staff_type', value)}
        />

        <Field label={text.department} htmlFor={id('department')} hint={text.departmentHint} error={errors.department}>
          <Select id={id('department')} value={draft.department} onChange={(event) => set('department', event.target.value)}>
            {directory.departments.map((department) => (
              <option key={department.department} value={department.department}>
                {department.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field label={text.license} htmlFor={id('license')} hint={text.licenseHint} error={errors.license_no}>
          <Input id={id('license')} value={draft.license_no} maxLength={64} onChange={(event) => set('license_no', event.target.value)} />
        </Field>
      </div>

      {directory.roles ? (
        <RolePicker
          roles={directory.roles}
          callerScopes={directory.callerScopes}
          selected={draft.roles}
          error={errors.roles}
          onChange={(roles) => set('roles', roles)}
        />
      ) : (
        <p className="text-sm text-ink-500">{text.rolesUnavailable}</p>
      )}

      {(Object.keys(branchOptions).length > 1 || directory.branchBound) && (
        <CheckboxGroup
          legend={text.branches}
          hint={directory.branchBound ? undefined : text.branchesHint}
          error={errors.branches}
          options={branchOptions}
          selected={draft.branches}
          onChange={(branches) => set('branches', branches)}
        />
      )}

      <FormFooter pending={pending} message={message} onCancel={onDone} />
    </form>
  );
}

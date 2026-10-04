'use client';

import { useState, type FormEvent } from 'react';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replaceRolePermissionsAction } from '../../actions/role-actions';
import { roleSettings } from '../../content/role-settings';
import { EVERY_SCOPE } from '../../lib/scope-groups';
import type { PermissionGroup, Role } from '../../types/role.types';
import { ScopePicker } from './scope-picker';

interface RolePermissionsFormProps {
  role: Role;
  groups: PermissionGroup[];
  callerScopes: string[];
  onDone: () => void;
}

/** A role's scopes, replaced as a whole. Everyone holding the role changes with it. */
export function RolePermissionsForm({ role, groups, callerScopes, onDone }: RolePermissionsFormProps) {
  const text = roleSettings.form;
  const [scopes, setScopes] = useState(role.scopes);
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => replaceRolePermissionsAction({ id: role.id, version: role.version, scopes }), onDone);
  }

  return (
    <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
      <ScopePicker groups={groups} callerScopes={callerScopes} selected={scopes} error={errors.scopes} onChange={setScopes} />

      {scopes.length === 0 && <p className="text-sm text-amber-800">{text.emptyWarning}</p>}
      {role.scopes.includes(EVERY_SCOPE) && <p className="text-sm text-ink-500">{text.lockoutNote}</p>}

      <FormFooter pending={pending} message={message} onCancel={onDone} />
    </form>
  );
}

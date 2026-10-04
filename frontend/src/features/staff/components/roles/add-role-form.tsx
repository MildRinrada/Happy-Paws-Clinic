'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createRoleAction } from '../../actions/role-actions';
import { roleSettings } from '../../content/role-settings';
import type { PermissionGroup } from '../../types/role.types';
import { ScopePicker } from './scope-picker';

interface AddRoleFormProps {
  groups: PermissionGroup[];
  callerScopes: string[];
}

/**
 * A clinic-defined role: a name and at least one scope. The name becomes the key the API speaks
 * (`"roles": ["senior_nurse"]`) and there is no endpoint to rename it, so the hint says so.
 */
export function AddRoleForm({ groups, callerScopes }: AddRoleFormProps) {
  const text = roleSettings.form;
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [scopes, setScopes] = useState<string[]>([]);
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  if (!open) {
    return (
      <div className="px-5 py-4">
        <Button onClick={() => setOpen(true)}>{roleSettings.actions.add}</Button>
      </div>
    );
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      // Sent as typed: the backend refuses a name with spaces at either end and says so under it.
      () => createRoleAction({ name, scopes }),
      () => {
        setName('');
        setScopes([]);
        setOpen(false);
      },
    );
  }

  return (
    <div className="px-5 py-4">
      <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
        <Field label={text.name} htmlFor="new-role-name" hint={text.nameHint} error={errors.name}>
          <Input id="new-role-name" value={name} minLength={2} maxLength={64} required className="font-mono sm:max-w-80" onChange={(event) => setName(event.target.value)} />
        </Field>

        <ScopePicker groups={groups} callerScopes={callerScopes} selected={scopes} error={errors.scopes} onChange={setScopes} />

        <FormFooter pending={pending} message={message} onCancel={() => setOpen(false)} submitLabel={formMessages.add} pendingLabel={formMessages.adding} />
      </form>
    </div>
  );
}

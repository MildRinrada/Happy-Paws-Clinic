'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { changePasswordAction } from '@/features/account/actions/account-actions';
import { portalAccount } from '../../content/portal-account';
import { TextField } from '../shared/text-field';
import { FormFooter, type FormStatus } from './form-footer';

/** Current password, then the new one twice. Saving signs every other device out. */
export function PasswordForm() {
  const text = portalAccount.password;
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [errors, setErrors] = useState<{ current?: string; next?: string }>({});
  const [status, setStatus] = useState<FormStatus>({ kind: 'idle' });
  const [pending, startTransition] = useTransition();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus({ kind: 'idle' });

    if (next.length < 8) return setErrors({ next: text.tooShort });
    if (next !== confirm) return setErrors({ next: text.mismatch });
    setErrors({});

    startTransition(async () => {
      try {
        const result = await changePasswordAction({ currentPassword: current, newPassword: next });

        if (result.ok) {
          setCurrent('');
          setNext('');
          setConfirm('');
          setStatus({ kind: 'saved', message: text.done });
        } else if (result.error.field === 'current_password') {
          setErrors({ current: text.wrongCurrent });
        } else if (result.error.field === 'new_password') {
          setErrors({ next: result.error.message });
        } else {
          setStatus({ kind: 'error', message: result.error.message });
        }
      } catch {
        setStatus({ kind: 'error', message: portalAccount.unreachable });
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-4">
      <TextField
        id="pw-current"
        type="password"
        label={text.current}
        value={current}
        onChange={(e) => setCurrent(e.target.value)}
        autoComplete="current-password"
        required
        error={errors.current}
      />
      <TextField
        id="pw-next"
        type="password"
        label={text.next}
        hint={text.nextHint}
        value={next}
        onChange={(e) => setNext(e.target.value)}
        autoComplete="new-password"
        minLength={8}
        maxLength={256}
        required
        error={errors.next}
      />
      <TextField
        id="pw-confirm"
        type="password"
        label={text.confirm}
        value={confirm}
        onChange={(e) => setConfirm(e.target.value)}
        autoComplete="new-password"
        required
      />
      <FormFooter pending={pending} status={status} label={text.submit} />
    </form>
  );
}

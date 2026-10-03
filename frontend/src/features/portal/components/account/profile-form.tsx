'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { updateProfileAction } from '@/features/account/actions/account-actions';
import type { Me } from '@/features/account/types/account.types';
import { portalAccount } from '../../content/portal-account';
import { refusalMessage } from '../../lib/form-error';
import { TextField } from '../shared/text-field';
import { FormFooter, type FormStatus } from './form-footer';

interface ProfileFormProps {
  me: Me;
  version?: number;
  onSaved: (version?: number) => void;
}

const digits = (value: string) => value.replace(/\D/g, '');

/**
 * Name, email and phone. Changing the email or the phone (the two ways in) asks for the current
 * password, as the backend requires; the box appears only once one of them has been changed.
 */
export function ProfileForm({ me, version, onSaved }: ProfileFormProps) {
  const text = portalAccount.profile;
  const [saved, setSaved] = useState({ email: me.email ?? '', phone: me.phone ?? '' });
  const [displayName, setDisplayName] = useState(me.display_name);
  const [email, setEmail] = useState(me.email ?? '');
  const [phone, setPhone] = useState(me.phone ?? '');
  const [currentPassword, setCurrentPassword] = useState('');
  const [fieldError, setFieldError] = useState<{ field?: string; message: string }>();
  const [status, setStatus] = useState<FormStatus>({ kind: 'idle' });
  const [pending, startTransition] = useTransition();

  const contactChanged = email.trim() !== saved.email || digits(phone) !== digits(saved.phone);
  const errorFor = (field: string) => (fieldError?.field === field ? fieldError.message : undefined);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFieldError(undefined);
    setStatus({ kind: 'idle' });

    startTransition(async () => {
      try {
        const result = await updateProfileAction({
          version,
          displayName,
          email,
          phone,
          currentPassword: contactChanged ? currentPassword : undefined,
        });

        if (result.ok) {
          setSaved({ email: email.trim(), phone });
          setCurrentPassword('');
          setStatus({ kind: 'saved' });
          onSaved(result.version);
          return;
        }

        const message = refusalMessage(result.error);
        if (result.error.field && ['email', 'phone', 'current_password'].includes(result.error.field)) {
          setFieldError({ field: result.error.field, message });
        } else {
          setStatus({ kind: 'error', message });
        }
      } catch {
        setStatus({ kind: 'error', message: portalAccount.unreachable });
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-4">
      <TextField id="me-name" label={text.displayName} value={displayName} onChange={(e) => setDisplayName(e.target.value)} maxLength={200} required />
      <TextField id="me-email" type="email" label={text.email} value={email} onChange={(e) => setEmail(e.target.value)} maxLength={256} autoComplete="email" error={errorFor('email')} />
      <TextField
        id="me-phone"
        type="tel"
        label={text.phone}
        hint={text.phoneHint}
        value={phone}
        onChange={(e) => setPhone(e.target.value)}
        maxLength={30}
        autoComplete="tel"
        error={errorFor('phone')}
      />

      {contactChanged && (
        <TextField
          id="me-current-password"
          type="password"
          label={text.currentPassword}
          hint={text.currentPasswordHint}
          value={currentPassword}
          onChange={(e) => setCurrentPassword(e.target.value)}
          autoComplete="current-password"
          required
          error={errorFor('current_password')}
        />
      )}

      <FormFooter pending={pending} status={status} />
    </form>
  );
}

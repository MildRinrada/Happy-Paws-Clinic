'use client';

import { useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Input, Select } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateMyProfileAction } from '../../actions/my-account-actions';
import { profileSettings } from '../../content/profile-settings';
import { emailChanged, phoneChanged, profileChanges, profileValues, type ProfileValues } from '../../lib/profile-changes';
import type { Me } from '../../types/account.types';
import { NotificationChannelsField } from './notification-channels-field';

/**
 * Name, sign-in email, phone, language and message channels. Only what changed is sent. A new email
 * or phone asks for the current password, as the backend requires; the box appears only then, so
 * renaming oneself does not need it.
 */
export function ProfileForm({ me }: { me: Me }) {
  const text = profileSettings;
  const [stored, setStored] = useState(() => profileValues(me));
  const [values, setValues] = useState(stored);
  const [version, setVersion] = useState(me.version);
  const [currentPassword, setCurrentPassword] = useState('');
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);

  const needsPassword = emailChanged(stored, values) || phoneChanged(stored, values);

  const set = <K extends keyof ProfileValues>(field: K, value: ProfileValues[K]) => {
    touch();
    setValues((current) => ({ ...current, [field]: value }));
  };

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const changes = {
      ...profileChanges(stored, values),
      current_password: needsPassword ? currentPassword : undefined,
    };

    submit(
      () => updateMyProfileAction({ version, changes }),
      (result) => {
        setStored(result.values);
        setValues(result.values);
        setVersion(result.version);
        setCurrentPassword('');
      },
    );
  }

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        <form onSubmit={save} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Field label={text.displayName} htmlFor="me-name" hint={text.displayNameHint} error={errors.display_name}>
                <Input
                  id="me-name"
                  value={values.display_name}
                  maxLength={200}
                  required
                  autoComplete="name"
                  aria-invalid={Boolean(errors.display_name)}
                  onChange={(event) => set('display_name', event.target.value)}
                />
              </Field>
            </div>

            <Field label={text.email} htmlFor="me-email" hint={text.emailHint} error={errors.email}>
              <Input
                id="me-email"
                type="email"
                value={values.email}
                maxLength={256}
                required={stored.email !== ''}
                autoComplete="email"
                aria-invalid={Boolean(errors.email)}
                onChange={(event) => set('email', event.target.value)}
              />
            </Field>

            <Field label={text.phone} htmlFor="me-phone" hint={text.phoneHint} error={errors.phone}>
              <Input
                id="me-phone"
                type="tel"
                inputMode="tel"
                value={values.phone}
                maxLength={32}
                autoComplete="tel"
                aria-invalid={Boolean(errors.phone)}
                onChange={(event) => set('phone', event.target.value)}
              />
            </Field>

            <Field label={text.language} htmlFor="me-language" error={errors.language}>
              <Select
                id="me-language"
                value={values.language}
                onChange={(event) => set('language', event.target.value as ProfileValues['language'])}
              >
                {Object.entries(text.languages).map(([code, name]) => (
                  <option key={code} value={code}>
                    {name}
                  </option>
                ))}
              </Select>
            </Field>

            <div className="sm:col-span-2">
              <NotificationChannelsField
                value={values.notifications}
                onChange={(notifications) => set('notifications', notifications)}
                error={errors.notifications}
              />
            </div>

            {needsPassword && (
              <div className="sm:col-span-2">
                <Field label={text.currentPassword} htmlFor="me-current-password" hint={text.currentPasswordHint} error={errors.current_password}>
                  <Input
                    id="me-current-password"
                    type="password"
                    value={currentPassword}
                    maxLength={256}
                    required
                    autoComplete="current-password"
                    aria-invalid={Boolean(errors.current_password)}
                    onChange={(event) => {
                      touch();
                      setCurrentPassword(event.target.value);
                    }}
                  />
                </Field>
              </div>
            )}
          </div>

          <FormFooter pending={pending} message={message} saved={saved} />
        </form>
      </CardBody>
    </Card>
  );
}

'use client';

import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { changeMyPasswordAction } from '../../actions/my-account-actions';
import { passwordSettings } from '../../content/password-settings';

const EMPTY = { current_password: '', new_password: '', confirm: '' };

type Values = typeof EMPTY;

/** What can be checked without the backend, so a typo costs no round trip. The rest is the backend's. */
function check(values: Values): Record<string, string> {
  if (values.new_password.length < 8) return { new_password: passwordSettings.tooShort };
  if (values.new_password !== values.confirm) return { confirm: passwordSettings.mismatch };
  return {};
}

/**
 * Current password, then the new one twice. The backend signs every other device out and keeps
 * this one, so the person stays on the page and the device list below shrinks to this browser.
 */
export function PasswordForm() {
  const text = passwordSettings;
  const [values, setValues] = useState(EMPTY);
  const [local, setLocal] = useState<Record<string, string>>({});
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);
  const shown = { ...errors, ...local };

  const set = (field: keyof Values, value: string) => {
    touch();
    setLocal({});
    setValues((current) => ({ ...current, [field]: value }));
  };

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const problems = check(values);
    setLocal(problems);
    if (Object.keys(problems).length > 0) return;

    submit(
      () => changeMyPasswordAction({ current_password: values.current_password, new_password: values.new_password }),
      () => setValues(EMPTY),
    );
  }

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        <form onSubmit={save} className="space-y-4" noValidate>
          <p className="text-sm text-ink-600">{text.intro}</p>

          {saved && <Alert tone="success">{text.done}</Alert>}

          <Field label={text.current} htmlFor="pw-current" error={shown.current_password}>
            <Input
              id="pw-current"
              type="password"
              value={values.current_password}
              maxLength={256}
              required
              autoComplete="current-password"
              aria-invalid={Boolean(shown.current_password)}
              onChange={(event) => set('current_password', event.target.value)}
            />
          </Field>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={text.next} htmlFor="pw-next" hint={text.nextHint} error={shown.new_password}>
              <Input
                id="pw-next"
                type="password"
                value={values.new_password}
                minLength={8}
                maxLength={256}
                required
                autoComplete="new-password"
                aria-invalid={Boolean(shown.new_password)}
                onChange={(event) => set('new_password', event.target.value)}
              />
            </Field>

            <Field label={text.confirm} htmlFor="pw-confirm" error={shown.confirm}>
              <Input
                id="pw-confirm"
                type="password"
                value={values.confirm}
                maxLength={256}
                required
                autoComplete="new-password"
                aria-invalid={Boolean(shown.confirm)}
                onChange={(event) => set('confirm', event.target.value)}
              />
            </Field>
          </div>

          <FormFooter pending={pending} message={message} submitLabel={text.submit} pendingLabel={text.submitting} />
        </form>
      </CardBody>
    </Card>
  );
}

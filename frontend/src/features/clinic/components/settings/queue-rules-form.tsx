'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input, Select } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateQueueSettingsAction } from '../../actions/clinic-rules-actions';
import { rulesSettings } from '../../content/rules-settings';
import type { QueueSettings, QueueSettingsInput } from '../../types/settings.types';

const DIGITS = [2, 3, 4, 5];

/** How a queue number is printed. The example comes back from the backend, which formats it. */
export function QueueRulesForm({ settings }: { settings: QueueSettings }) {
  const text = rulesSettings.queue;
  const [values, setValues] = useState<QueueSettingsInput>({
    number_prefix: settings.number_prefix,
    number_digits: settings.number_digits,
    reset_daily: settings.reset_daily,
  });
  const [version, setVersion] = useState(settings.version);
  const [example, setExample] = useState(settings.example);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);

  const set = <K extends keyof QueueSettingsInput>(field: K, value: QueueSettingsInput[K]) => {
    touch();
    setValues((current) => ({ ...current, [field]: value }));
  };

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () => updateQueueSettingsAction({ version, values }),
      (stored) => {
        setVersion(stored.version);
        setExample(stored.example);
      },
    );
  }

  return (
    <Card>
      <CardHeader title={text.heading} aside={<span className="font-mono text-lg font-semibold text-brand-800" title={text.example}>{example}</span>} />
      <CardBody>
        <form onSubmit={save} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={text.prefix} htmlFor="queue-prefix" hint={text.prefixHint} error={errors.number_prefix}>
              <Input
                id="queue-prefix"
                value={values.number_prefix}
                maxLength={3}
                className="max-w-24 font-mono uppercase"
                onChange={(event) => set('number_prefix', event.target.value.toUpperCase())}
              />
            </Field>

            <Field label={text.digits} htmlFor="queue-digits" error={errors.number_digits}>
              <Select id="queue-digits" value={values.number_digits} className="max-w-24" onChange={(event) => set('number_digits', Number(event.target.value))}>
                {DIGITS.map((digits) => (
                  <option key={digits} value={digits}>
                    {digits}
                  </option>
                ))}
              </Select>
            </Field>
          </div>

          <Checkbox label={text.resetDaily} checked={values.reset_daily} onChange={(event) => set('reset_daily', event.target.checked)} />

          <div className="flex flex-wrap items-center justify-end gap-3">
            <FormStatus message={message} saved={saved} savedText={formMessages.saved} />
            <Button type="submit" disabled={pending}>
              {pending ? formMessages.saving : formMessages.save}
            </Button>
          </div>
        </form>
      </CardBody>
    </Card>
  );
}

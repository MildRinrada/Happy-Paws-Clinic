'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input, Select } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createAutomationAction } from '../actions/automation-actions';
import { automationChannelLabels, triggerChoices } from '../content/automation-labels';
import { automationsPage } from '../content/automations-page';
import { initialCondition, initialTiming, toCreateInput } from '../lib/automation-form';
import type { AutomationChannel, AutomationTriggerType } from '../types/automation.types';
import { ConditionFields } from './condition-fields';
import { TimingFields } from './timing-fields';

const CHANNELS: AutomationChannel[] = ['auto', 'line', 'sms', 'email'];

/** A new rule: when, for whom, on which channel, and whether it runs now or waits for a preview. */
export function CreateAutomationForm({ branch }: { branch?: { id: string; name: string } }) {
  const text = automationsPage.create;
  const [name, setName] = useState('');
  const [type, setType] = useState<AutomationTriggerType>('vaccine_due');
  const [timing, setTiming] = useState(initialTiming);
  const [condition, setCondition] = useState(initialCondition);
  const [channel, setChannel] = useState<AutomationChannel>('auto');
  const [enabled, setEnabled] = useState(false);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit({ conflict: formMessages.conflict, unreachable: formMessages.unreachable });

  function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () => createAutomationAction(toCreateInput({ name, type, timing, condition, channel, enabled, branchId: branch?.id })),
      () => setName(''),
    );
  }

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        <form onSubmit={create} onChange={touch} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={text.name} htmlFor="automation-name" error={errors.name}>
              <Input id="automation-name" value={name} maxLength={120} placeholder={text.namePlaceholder} onChange={(event) => setName(event.target.value)} />
            </Field>
            <Field label={text.trigger} htmlFor="automation-trigger" error={errors['trigger.type']}>
              <Select id="automation-trigger" value={type} onChange={(event) => setType(event.target.value as AutomationTriggerType)}>
                {triggerChoices.map((choice) => (
                  <option key={choice.value} value={choice.value}>
                    {choice.label}
                  </option>
                ))}
              </Select>
            </Field>
          </div>

          <TimingFields trigger={type} value={timing} errors={errors} onChange={setTiming} />
          <ConditionFields trigger={type} value={condition} errors={errors} onChange={setCondition} />

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={text.channel} htmlFor="automation-channel" error={errors['action.channel']}>
              <Select id="automation-channel" value={channel} onChange={(event) => setChannel(event.target.value as AutomationChannel)}>
                {CHANNELS.map((value) => (
                  <option key={value} value={value}>
                    {automationChannelLabels[value]}
                  </option>
                ))}
              </Select>
            </Field>
            <Checkbox className="self-end" label={text.enabled} hint={text.enabledHint} checked={enabled} onChange={(event) => setEnabled(event.target.checked)} />
          </div>

          <div className="flex flex-wrap items-center justify-between gap-3">
            {branch && <p className="text-xs text-ink-500">{text.branchNote(branch.name)}</p>}
            <div className="ml-auto flex items-center gap-3">
              <FormStatus message={message ?? errors['condition.branch_id']} saved={saved} savedText={text.created} />
              <Button type="submit" disabled={pending || name.trim() === ''}>
                {pending ? text.submitting : text.submit}
              </Button>
            </div>
          </div>
        </form>
      </CardBody>
    </Card>
  );
}

'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateAutomationAction } from '../actions/automation-actions';
import { triggerLabels } from '../content/automation-labels';
import { automationsPage } from '../content/automations-page';
import { conditionOf, timingOf, toUpdateValues } from '../lib/automation-form';
import type { Automation, AutomationChannel } from '../types/automation.types';
import { ChannelField } from './channel-field';
import { ConditionFields } from './condition-fields';
import { LapsedConsentNote } from './lapsed-consent-note';
import { ReviewConsentNote } from './review-consent-note';
import { RuleNameField } from './rule-name-field';
import { TimingFields } from './timing-fields';

interface EditAutomationFormProps {
  rule: Automation;
  onDone: () => void;
  onCancel: () => void;
}

/**
 * An existing rule's name, timing, conditions and channel, in the create form's own fields. What
 * the rule watches is shown, not offered: changing it would make it a different rule.
 */
export function EditAutomationForm({ rule, onDone, onCancel }: EditAutomationFormProps) {
  const text = automationsPage.edit;
  const prefix = `automation-${rule.id}`;
  const [name, setName] = useState(rule.name);
  const [timing, setTiming] = useState(() => timingOf(rule));
  const [condition, setCondition] = useState(() => conditionOf(rule));
  const [channel, setChannel] = useState<AutomationChannel>(rule.action.channel);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit({ conflict: formMessages.conflict, unreachable: formMessages.unreachable });

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () =>
        updateAutomationAction({
          id: rule.id,
          version: rule.version,
          values: toUpdateValues({ name, type: rule.trigger.type, timing, condition, channel }),
        }),
      onDone,
    );
  }

  return (
    <form onSubmit={save} onChange={touch} className="space-y-4 rounded-lg border border-ink-100 bg-ink-50 p-4">
      <p className="text-xs text-ink-500">{text.watches(triggerLabels[rule.trigger.type])}</p>
      {rule.trigger.type === 'customer_lapsed' && <LapsedConsentNote />}
      {rule.trigger.type === 'visit_completed' && <ReviewConsentNote />}
      <div className="grid gap-4 sm:grid-cols-2">
        <RuleNameField id={`${prefix}-name`} value={name} error={errors.name} onChange={setName} />
        <ChannelField id={`${prefix}-channel`} value={channel} error={errors['action.channel']} onChange={setChannel} />
      </div>
      <TimingFields idPrefix={prefix} trigger={rule.trigger.type} value={timing} errors={errors} onChange={setTiming} />
      <ConditionFields idPrefix={prefix} trigger={rule.trigger.type} value={condition} errors={errors} onChange={setCondition} />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-xs text-ink-500">{text.noRepeat}</p>
        <div className="ml-auto flex items-center gap-3">
          <FormStatus message={message ?? errors['trigger.type']} saved={saved} savedText={text.saved} />
          <Button variant="ghost" size="sm" disabled={pending} onClick={onCancel}>
            {text.cancel}
          </Button>
          <Button type="submit" size="sm" disabled={pending || name.trim() === ''}>
            {pending ? text.saving : text.save}
          </Button>
        </div>
      </div>
    </form>
  );
}

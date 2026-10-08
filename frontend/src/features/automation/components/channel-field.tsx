'use client';

import { Field, Select } from '@/components/ui/field';
import { automationChannelLabels } from '../content/automation-labels';
import { automationsPage } from '../content/automations-page';
import type { AutomationChannel } from '../types/automation.types';

const CHANNELS: AutomationChannel[] = ['auto', 'line', 'sms', 'email'];

interface ChannelFieldProps {
  id: string;
  value: AutomationChannel;
  error?: string;
  onChange: (value: AutomationChannel) => void;
}

/** Which way the message goes: the usual LINE, SMS, email order, or one channel only. */
export function ChannelField({ id, value, error, onChange }: ChannelFieldProps) {
  return (
    <Field label={automationsPage.create.channel} htmlFor={id} error={error}>
      <Select id={id} value={value} onChange={(event) => onChange(event.target.value as AutomationChannel)}>
        {CHANNELS.map((channel) => (
          <option key={channel} value={channel}>
            {automationChannelLabels[channel]}
          </option>
        ))}
      </Select>
    </Field>
  );
}

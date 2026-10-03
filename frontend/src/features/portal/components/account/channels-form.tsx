'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { updateChannelsAction } from '@/features/account/actions/account-actions';
import { portalAccount } from '../../content/portal-account';
import { refusalMessage } from '../../lib/form-error';
import { FormFooter, type FormStatus } from './form-footer';
import { ToggleRow } from './toggle-row';

type Channels = { email: boolean; sms: boolean; line: boolean };

interface ChannelsFormProps {
  initial: Channels;
  version?: number;
  onSaved: (version?: number) => void;
}

const CHANNELS: (keyof Channels)[] = ['line', 'sms', 'email'];

/** Which channels the clinic may use to reach the owner. Whether it may at all is a consent, below. */
export function ChannelsForm({ initial, version, onSaved }: ChannelsFormProps) {
  const [channels, setChannels] = useState(initial);
  const [status, setStatus] = useState<FormStatus>({ kind: 'idle' });
  const [pending, startTransition] = useTransition();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus({ kind: 'idle' });

    startTransition(async () => {
      try {
        const result = await updateChannelsAction({ version, ...channels });

        if (result.ok) {
          setStatus({ kind: 'saved' });
          onSaved(result.version);
        } else {
          setStatus({ kind: 'error', message: refusalMessage(result.error) });
        }
      } catch {
        setStatus({ kind: 'error', message: portalAccount.unreachable });
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-2">
      <div className="divide-y divide-ink-100">
        {CHANNELS.map((channel) => (
          <ToggleRow
            key={channel}
            id={`channel-${channel}`}
            label={portalAccount.channels[channel]}
            checked={channels[channel]}
            onChange={(checked) => setChannels({ ...channels, [channel]: checked })}
          />
        ))}
      </div>
      <FormFooter pending={pending} status={status} />
    </form>
  );
}

import { Checkbox } from '@/components/ui/checkbox';
import { profileSettings } from '../../content/profile-settings';
import type { NotificationChannels } from '../../types/account.types';

interface NotificationChannelsFieldProps {
  value: NotificationChannels;
  onChange: (value: NotificationChannels) => void;
  error?: string;
}

const CHANNELS: (keyof NotificationChannels)[] = ['line', 'sms', 'email'];

/** Three independent switches. All three are always sent together, so none is left as it was by accident. */
export function NotificationChannelsField({ value, onChange, error }: NotificationChannelsFieldProps) {
  const text = profileSettings;

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium text-ink-700">{text.channels}</legend>
      <p className="text-sm text-ink-500">{text.channelsHint}</p>
      <div className="flex flex-wrap gap-x-6 gap-y-2">
        {CHANNELS.map((channel) => (
          <Checkbox
            key={channel}
            label={text.channelNames[channel]}
            checked={value[channel]}
            onChange={(event) => onChange({ ...value, [channel]: event.target.checked })}
          />
        ))}
      </div>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </fieldset>
  );
}

import { Checkbox } from '@/components/ui/checkbox';
import { labelOf, templateLabels } from '@/features/notifications/content/delivery-labels';
import { lineSettingsPage } from '../content/line-settings';

interface LineTypeSwitchesProps {
  types: Record<string, boolean>;
  onChange: (key: string, on: boolean) => void;
}

/** One checkbox per kind of message: on, it may go out on LINE; off, it takes SMS or email. */
export function LineTypeSwitches({ types, onChange }: LineTypeSwitchesProps) {
  const text = lineSettingsPage.sending;

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium text-ink-700">{text.types}</legend>
      <p className="text-sm text-ink-500">{text.typesHint}</p>
      <div className="grid gap-2 sm:grid-cols-2">
        {Object.entries(types).map(([key, on]) => (
          <Checkbox key={key} label={labelOf(templateLabels, key)} hint={key} checked={on} onChange={(event) => onChange(key, event.target.checked)} />
        ))}
      </div>
    </fieldset>
  );
}

import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/field';
import { hoursSettings } from '../../content/hours-settings';
import { WEEKDAY_LABELS } from '../../lib/weekdays';
import type { DayHoursInput } from '../../types/settings.types';

interface DayHoursRowProps {
  day: DayHoursInput;
  error?: string;
  onChange: (day: DayHoursInput) => void;
}

/** One weekday: open and close times, or closed all day. */
export function DayHoursRow({ day, error, onChange }: DayHoursRowProps) {
  const text = hoursSettings.week;
  const label = WEEKDAY_LABELS[day.day];

  return (
    <li className="flex flex-wrap items-center gap-x-4 gap-y-2 px-5 py-3">
      <span className="w-24 shrink-0 font-medium text-ink-900">{label}</span>

      <div className="flex items-center gap-2">
        <Input
          type="time"
          aria-label={`${label} ${text.opens}`}
          value={day.opens ?? ''}
          disabled={day.closed}
          required={!day.closed}
          className="max-w-32"
          onChange={(event) => onChange({ ...day, opens: event.target.value })}
        />
        <span className="text-ink-400">-</span>
        <Input
          type="time"
          aria-label={`${label} ${text.closes}`}
          value={day.closes ?? ''}
          disabled={day.closed}
          required={!day.closed}
          className="max-w-32"
          onChange={(event) => onChange({ ...day, closes: event.target.value })}
        />
      </div>

      <Checkbox label={text.closed} checked={day.closed} onChange={(event) => onChange({ ...day, closed: event.target.checked })} />

      {error && (
        <p role="alert" className="w-full text-sm text-red-700">
          {error}
        </p>
      )}
    </li>
  );
}

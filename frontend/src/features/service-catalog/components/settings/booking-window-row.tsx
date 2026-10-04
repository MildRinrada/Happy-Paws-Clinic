import { Button } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { WEEKDAY_LABELS } from '@/features/clinic/lib/weekdays';
import { serviceSettings } from '../../content/service-settings';
import type { BookingWindow } from '../../types/managed-service.types';

interface BookingWindowRowProps {
  window: BookingWindow;
  error?: string;
  onChange: (window: BookingWindow) => void;
  onRemove: () => void;
}

/** One weekly window in which customers may book this service themselves. */
export function BookingWindowRow({ window, error, onChange, onRemove }: BookingWindowRowProps) {
  const text = serviceSettings.availability;

  return (
    <li className="space-y-1">
      <div className="flex flex-wrap items-center gap-2">
        <Select aria-label={text.day} value={window.day} className="max-w-36" onChange={(event) => onChange({ ...window, day: event.target.value })}>
          {Object.entries(WEEKDAY_LABELS).map(([day, label]) => (
            <option key={day} value={day}>
              {label}
            </option>
          ))}
        </Select>
        <Input type="time" aria-label={text.from} value={window.from} required className="max-w-32" onChange={(event) => onChange({ ...window, from: event.target.value })} />
        <span className="text-ink-400">-</span>
        <Input type="time" aria-label={text.to} value={window.to} required className="max-w-32" onChange={(event) => onChange({ ...window, to: event.target.value })} />
        <Button variant="ghost" size="sm" onClick={onRemove}>
          {text.removeWindow}
        </Button>
      </div>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </li>
  );
}

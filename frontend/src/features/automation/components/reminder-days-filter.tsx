import { buttonClass } from '@/components/ui/button';
import { Select } from '@/components/ui/field';
import { remindersPage } from '../content/reminders-page';

/** How far ahead to look. A plain GET form, so the choice lives in the address. */
export function ReminderDaysFilter({ days }: { days: number }) {
  const text = remindersPage;

  return (
    <form method="get" action="/notifications/reminders" className="flex flex-wrap items-end gap-3">
      <label className="space-y-1 text-xs text-ink-600">
        <span className="block">{text.days}</span>
        <Select name="days" defaultValue={days} className="h-9 w-28">
          {text.dayChoices.map((choice) => (
            <option key={choice} value={choice}>
              {text.dayLabel(choice)}
            </option>
          ))}
        </Select>
      </label>
      <button type="submit" className={buttonClass('secondary', 'sm')}>
        {text.apply}
      </button>
    </form>
  );
}

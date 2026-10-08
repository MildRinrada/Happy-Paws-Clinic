import { Button } from '@/components/ui/button';
import { Select } from '@/components/ui/field';
import { alertsPage } from '../content/alerts-page';
import { ALERT_WINDOWS } from '../lib/alert-windows';
import { inventoryPaths } from '../lib/inventory-paths';

/** How far ahead "ใกล้หมดอายุ" looks. A GET form, so the choice stays in the address. */
export function AlertsWindowForm({ withinDays }: { withinDays: number }) {
  return (
    <form method="get" action={inventoryPaths.alerts} className="flex items-end gap-3">
      <label className="block w-44 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{alertsPage.window}</span>
        <Select name="within_days" defaultValue={String(withinDays)}>
          {ALERT_WINDOWS.map((days) => (
            <option key={days} value={days}>
              {alertsPage.windowOption(days)}
            </option>
          ))}
        </Select>
      </label>
      <Button type="submit" variant="secondary">
        {alertsPage.apply}
      </Button>
    </form>
  );
}

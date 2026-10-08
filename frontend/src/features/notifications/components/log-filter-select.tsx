import { Select } from '@/components/ui/field';
import { deliveryLogPage } from '../content/delivery-log';

interface LogFilterSelectProps {
  name: string;
  label: string;
  value?: string;
  options: string[];
  labels: Record<string, string>;
}

/** One labelled filter of the log, with "ทั้งหมด" first. */
export function LogFilterSelect({ name, label, value, options, labels }: LogFilterSelectProps) {
  return (
    <label className="space-y-1 text-sm font-medium text-ink-700">
      <span className="block">{label}</span>
      <Select name={name} defaultValue={value ?? ''} className="w-40">
        <option value="">{deliveryLogPage.filters.all}</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {labels[option] ?? option}
          </option>
        ))}
      </Select>
    </label>
  );
}

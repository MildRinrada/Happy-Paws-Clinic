import { Field, Input } from '@/components/ui/field';
import { staffTypeLabel } from '../../content/staff-labels';
import { staffSettings } from '../../content/staff-settings';
import type { StaffTypeOption } from '../../types/staff.types';

interface StaffTypeInputProps {
  id: string;
  value: string;
  options: StaffTypeOption[];
  error?: string;
  onChange: (value: string) => void;
}

/**
 * A position. Free text with suggestions, because a staff type is defined by using it: the API has
 * no endpoint that creates one, and lists whatever is in use plus four suggestions.
 */
export function StaffTypeInput({ id, value, options, error, onChange }: StaffTypeInputProps) {
  return (
    <Field label={staffSettings.edit.staffType} htmlFor={id} hint={staffSettings.edit.staffTypeHint} error={error}>
      <Input id={id} list={`${id}-options`} value={value} maxLength={64} required onChange={(event) => onChange(event.target.value)} />
      <datalist id={`${id}-options`}>
        {options.map((option) => (
          <option key={option.staff_type} value={option.staff_type}>
            {staffTypeLabel(option.staff_type)}
          </option>
        ))}
      </datalist>
    </Field>
  );
}

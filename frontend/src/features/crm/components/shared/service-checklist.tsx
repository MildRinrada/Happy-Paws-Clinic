import { CheckboxGroup } from '@/components/ui/checkbox-group';
import type { ServiceOption } from '../../types/crm.types';

interface Props {
  legend: string;
  hint?: string;
  error?: string;
  /** Said instead of the list when there is nothing to pick. */
  emptyText: string;
  services: ServiceOption[];
  selected: string[];
  onChange: (selected: string[]) => void;
}

/** Ticks against the service catalogue, sent as a list of service ids. */
export function ServiceChecklist({ legend, hint, error, emptyText, services, selected, onChange }: Props) {
  if (services.length === 0) {
    return (
      <div className="space-y-1">
        <p className="text-sm font-medium text-ink-700">{legend}</p>
        <p className="text-sm text-ink-500">{emptyText}</p>
      </div>
    );
  }

  return (
    <CheckboxGroup
      legend={legend}
      hint={hint}
      error={error}
      options={Object.fromEntries(services.map((service) => [service.id, service.name]))}
      selected={selected}
      onChange={onChange}
    />
  );
}

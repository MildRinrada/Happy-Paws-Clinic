import { buttonClass } from '@/components/ui/button';
import { Select } from '@/components/ui/field';
import { packageStatusLabels } from '../../content/crm-labels';
import { packageLookupText } from '../../content/package-lookup';
import type { PackageStatus } from '../../types/crm.types';

/** The status filter of the package lookup, a plain GET form that keeps the customer. */
export function PackageStatusFilter({ customerId, status }: { customerId: string; status?: PackageStatus }) {
  const text = packageLookupText;

  return (
    <form method="get" aria-label={text.filterLabel} className="flex flex-wrap items-end gap-3">
      <input type="hidden" name="customer_id" value={customerId} />
      <label className="block w-44 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.status}</span>
        <Select name="status" defaultValue={status ?? ''}>
          <option value="">{text.any}</option>
          {(Object.keys(packageStatusLabels) as PackageStatus[]).map((value) => (
            <option key={value} value={value}>
              {packageStatusLabels[value].text}
            </option>
          ))}
        </Select>
      </label>
      <button type="submit" className={buttonClass('secondary', 'md')}>
        {text.apply}
      </button>
    </form>
  );
}

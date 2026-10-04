import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Field, Select } from '@/components/ui/field';
import { roleLabel } from '../../content/staff-labels';
import { staffSettings } from '../../content/staff-settings';
import type { Role } from '../../types/role.types';
import type { DepartmentOption, StaffListFilters } from '../../types/staff.types';

interface StaffFilterFormProps {
  filters: StaffListFilters;
  branches: Record<string, string>;
  departments: DepartmentOption[];
  /** `null` without `role:read`; the role filter is then left out. */
  roles: Role[] | null;
}

/**
 * Branch, department and role, sent as a plain GET form: the filters live in the URL, so a
 * filtered list can be bookmarked and needs no script to work.
 */
export function StaffFilterForm({ filters, branches, departments, roles }: StaffFilterFormProps) {
  const text = staffSettings.filters;
  const branchIds = Object.keys(branches);

  return (
    <form method="get" action="/settings/staff" className="flex flex-wrap items-end gap-3">
      {filters.status && <input type="hidden" name="status" value={filters.status} />}

      {branchIds.length > 1 && (
        <div className="w-full sm:w-44">
          <Field label={text.branch} htmlFor="staff-filter-branch">
            <Select id="staff-filter-branch" name="branch" defaultValue={filters.branch ?? ''}>
              <option value="">{text.any}</option>
              {branchIds.map((id) => (
                <option key={id} value={id}>
                  {branches[id]}
                </option>
              ))}
            </Select>
          </Field>
        </div>
      )}

      <div className="w-full sm:w-44">
        <Field label={text.department} htmlFor="staff-filter-department">
          <Select id="staff-filter-department" name="department" defaultValue={filters.department ?? ''}>
            <option value="">{text.any}</option>
            {departments.map((department) => (
              <option key={department.department} value={department.department}>
                {department.name}
              </option>
            ))}
          </Select>
        </Field>
      </div>

      {roles && (
        <div className="w-full sm:w-44">
          <Field label={text.role} htmlFor="staff-filter-role">
            <Select id="staff-filter-role" name="role" defaultValue={filters.role ?? ''}>
              <option value="">{text.any}</option>
              {roles
                .filter((role) => role.assignable)
                .map((role) => (
                  <option key={role.id} value={role.name}>
                    {roleLabel(role.name)}
                  </option>
                ))}
            </Select>
          </Field>
        </div>
      )}

      <div className="flex items-center gap-2">
        <Button type="submit" variant="secondary">
          {text.apply}
        </Button>
        {(filters.branch || filters.department || filters.role) && (
          <Link href={filters.status ? `/settings/staff?status=${filters.status}` : '/settings/staff'} className="px-2 text-sm text-ink-600 hover:text-brand-700">
            {text.clear}
          </Link>
        )}
      </div>
    </form>
  );
}

import type { StaffListFilters, StaffStatus } from '../types/staff.types';

const statuses: readonly StaffStatus[] = ['active', 'invited', 'deactivated'];

const text = (value: string | string[] | undefined) => {
  const first = Array.isArray(value) ? value[0] : value;
  return first?.trim() || undefined;
};

/**
 * The page's query string as list filters. An unknown status is dropped rather than sent: the
 * backend would answer 422 for a value nobody can pick on this screen. Department and role pass
 * through, and the backend judges them.
 */
export function parseStaffFilters(params: Record<string, string | string[] | undefined>): StaffListFilters {
  const status = text(params.status);

  return {
    status: statuses.includes(status as StaffStatus) ? (status as StaffStatus) : undefined,
    branch: text(params.branch),
    department: text(params.department),
    role: text(params.role),
    cursor: text(params.cursor),
  };
}

/** A link back to this page with the given filters. Empty values are left out of the URL. */
export function staffPageHref(filters: StaffListFilters): string {
  const query = new URLSearchParams();

  for (const [key, value] of Object.entries(filters)) {
    if (value) query.set(key, value);
  }

  const search = query.toString();
  return search ? `/settings/staff?${search}` : '/settings/staff';
}

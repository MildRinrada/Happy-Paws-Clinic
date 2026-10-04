import type { Permission, PermissionGroup } from '../types/role.types';

/** The catalogue by resource, in the order the API lists it (alphabetical by scope). */
export function groupPermissions(permissions: Permission[]): PermissionGroup[] {
  const groups = new Map<string, Permission[]>();

  for (const permission of permissions) {
    groups.set(permission.resource, [...(groups.get(permission.resource) ?? []), permission]);
  }

  return [...groups.entries()].map(([resource, scopes]) => ({ resource, scopes }));
}

/** `consult` → `consult:*`, the grant of every action on a resource, present and future. */
export const resourceWildcard = (resource: string) => `${resource}:*`;

/** The unrestricted grant `clinic_owner` holds. */
export const EVERY_SCOPE = '*';

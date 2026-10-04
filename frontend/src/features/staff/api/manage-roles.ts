import { staffApi } from '@/lib/api/server-client';
import type { Permission, Role } from '../types/role.types';

/**
 * Roles and the scope catalogue. `role:read` to look, `role:manage` to change. The backend refuses
 * granting a scope the caller does not hold, and any change that would leave nobody with `*`.
 */

/** `GET /staff/roles` — the starter roles and the clinic's own. */
export async function listRoles(): Promise<Role[]> {
  const { data } = await staffApi().get<{ data: Role[] }>('/staff/roles');
  return data;
}

/** `GET /staff/permissions` — every scope a role may carry, the same list the writes check. */
export async function listPermissions(): Promise<Permission[]> {
  const { data } = await staffApi().get<{ data: Permission[] }>('/staff/permissions');
  return data;
}

/** `POST /staff/roles` — a name and at least one scope. */
export function createRole(input: { name: string; scopes: string[] }): Promise<Role> {
  return staffApi().post<Role>('/staff/roles', { body: input });
}

/** `PUT /staff/roles/{id}/permissions` — the whole scope set as it should be afterwards. */
export function replaceRolePermissions(id: string, version: number, scopes: string[]): Promise<Role> {
  return staffApi().put<Role>(`/staff/roles/${encodeURIComponent(id)}/permissions`, { body: { scopes }, ifMatch: version });
}

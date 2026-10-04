import { allows } from '@/lib/auth/scopes';
import type { Role } from '../types/role.types';

/**
 * The backend's ScopeGrantPolicy, read ahead of time so the screen can grey out what a save would
 * refuse: nobody hands out a scope they do not hold. `allows` already reads wildcards the way the
 * policy does, so `consult:*` is grantable only by someone holding `consult:*` or `*`, never by
 * holding every `consult:` scope one at a time.
 *
 * This only decides what to draw. The backend checks again and is the one that refuses.
 */
export function canGrantAll(callerScopes: readonly string[], scopes: readonly string[]): boolean {
  return scopes.every((scope) => allows(callerScopes, scope));
}

/**
 * Whether the caller may change someone holding these role names. The backend refuses changing or
 * deactivating a member who holds more than the caller, so an owner cannot be removed by an admin.
 * With no role list to look in (no `role:read`), the backend is left to decide.
 */
export function canChangeHolder(callerScopes: readonly string[], roleNames: readonly string[], roles: Role[] | null): boolean {
  if (!roles) return true;

  return roleNames.every((name) => {
    const role = roles.find((candidate) => candidate.name === name);
    return !role || canGrantAll(callerScopes, role.scopes);
  });
}

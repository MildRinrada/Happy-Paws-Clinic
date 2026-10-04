'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { roleSettings } from '../../content/role-settings';
import { canGrantAll } from '../../lib/scope-grants';
import type { PermissionGroup, Role } from '../../types/role.types';
import { RolePermissionsForm } from './role-permissions-form';
import { RoleSummary } from './role-summary';

interface RoleRowProps {
  role: Role;
  /** `null` when the catalogue could not be read; the role is then shown but not editable. */
  groups: PermissionGroup[] | null;
  callerScopes: string[];
  canManage: boolean;
}

/**
 * One role and its editor. The portal role is never editable. A role holding a scope the caller
 * lacks is locked too: the backend makes the caller hold everything the role had as well as
 * everything it is given, so an admin cannot strip the owner's `*`.
 */
export function RoleRow({ role, groups, callerScopes, canManage }: RoleRowProps) {
  const [editing, setEditing] = useState(false);
  const locked = !canGrantAll(callerScopes, role.scopes);
  const editable = canManage && role.assignable && groups !== null && !locked;

  return (
    <li className="space-y-3 px-5 py-4">
      <div className="flex flex-wrap items-start gap-3">
        <RoleSummary role={role} />
        {editable && (
          <Button variant={editing ? 'primary' : 'secondary'} size="sm" onClick={() => setEditing(!editing)}>
            {roleSettings.actions.edit}
          </Button>
        )}
      </div>

      {canManage && role.assignable && locked && <p className="text-xs text-ink-500">{roleSettings.locked}</p>}

      {editing && groups && <RolePermissionsForm role={role} groups={groups} callerScopes={callerScopes} onDone={() => setEditing(false)} />}
    </li>
  );
}

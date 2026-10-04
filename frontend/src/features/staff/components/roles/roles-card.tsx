import { Card, CardHeader } from '@/components/ui/card';
import { roleSettings } from '../../content/role-settings';
import { groupPermissions } from '../../lib/scope-groups';
import type { Permission, Role } from '../../types/role.types';
import { AddRoleForm } from './add-role-form';
import { RoleRow } from './role-row';

interface RolesCardProps {
  roles: Role[];
  /** `null` when the catalogue could not be read: roles still show, nothing is editable. */
  permissions: Permission[] | null;
  callerScopes: string[];
  canManage: boolean;
}

/** Every role with its scopes; starter roles first, then the clinic's own, then the portal one. */
export function RolesCard({ roles, permissions, callerScopes, canManage }: RolesCardProps) {
  const groups = permissions && groupPermissions(permissions);
  const rank = (role: Role) => (!role.assignable ? 2 : role.is_system ? 0 : 1);
  const ordered = [...roles].sort((a, b) => rank(a) - rank(b) || a.name.localeCompare(b.name));

  return (
    <Card>
      <CardHeader title={roleSettings.title} aside={<span className="text-sm text-ink-500">{roleSettings.count(roles.length)}</span>} />
      <p className="border-b border-ink-100 px-5 py-3 text-sm text-ink-600">{roleSettings.intro}</p>
      {canManage && !groups && <p className="border-b border-ink-100 px-5 py-3 text-sm text-amber-800">{roleSettings.catalogueUnavailable}</p>}
      {canManage && groups && <AddRoleForm groups={groups} callerScopes={callerScopes} />}

      <ul className="divide-y divide-ink-100 border-t border-ink-100">
        {ordered.map((role) => (
          // Keyed by version: a save reloads the page and the editor starts from what was stored.
          <RoleRow key={`${role.id}:${role.version}`} role={role} groups={groups} callerScopes={callerScopes} canManage={canManage} />
        ))}
      </ul>
    </Card>
  );
}

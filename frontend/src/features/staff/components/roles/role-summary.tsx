import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { roleSettings } from '../../content/role-settings';
import { roleLabel } from '../../content/staff-labels';
import { staffPageHref } from '../../lib/staff-filters';
import type { Role } from '../../types/role.types';
import { ScopeChips } from './scope-chips';

/** A role's name, kind and scopes, with a way to see who holds it. */
export function RoleSummary({ role }: { role: Role }) {
  const text = roleSettings;

  return (
    <div className="min-w-0 flex-1 space-y-2">
      <p className="flex flex-wrap items-center gap-2">
        <span className="font-medium text-ink-900">{roleLabel(role.name)}</span>
        <span className="font-mono text-xs text-ink-500">{role.name}</span>
        {!role.assignable ? (
          <Badge tone="amber">{text.badges.portal}</Badge>
        ) : role.is_system ? (
          <Badge>{text.badges.system}</Badge>
        ) : (
          <Badge tone="blue">{text.badges.custom}</Badge>
        )}
        {role.scopes.length === 0 && <Badge tone="red">{text.badges.empty}</Badge>}
        <span className="text-xs text-ink-500">{text.summary.scopes(role.scopes.length)}</span>
      </p>
      <ScopeChips scopes={role.scopes} />
      {role.assignable && (
        <Link href={staffPageHref({ role: role.name })} className="inline-block text-sm text-brand-700 hover:underline">
          {text.summary.members}
        </Link>
      )}
    </div>
  );
}

import { Checkbox } from '@/components/ui/checkbox';
import { allows } from '@/lib/auth/scopes';
import { roleSettings } from '../../content/role-settings';
import { actionLabel, resourceLabel } from '../../content/scope-labels';
import { resourceWildcard } from '../../lib/scope-groups';
import type { PermissionGroup } from '../../types/role.types';

interface ScopeGroupProps {
  group: PermissionGroup;
  callerScopes: string[];
  selected: Set<string>;
  onToggle: (scope: string, on: boolean) => void;
}

/**
 * One resource: a box for the whole resource (`consult:*`) and one per action. With the whole
 * resource ticked, each action is covered by it and shows ticked and locked.
 */
export function ScopeGroup({ group, callerScopes, selected, onToggle }: ScopeGroupProps) {
  const text = roleSettings.picker;
  const wildcard = resourceWildcard(group.resource);
  const whole = selected.has(wildcard);

  return (
    <fieldset className="space-y-2 rounded-lg border border-ink-100 p-3">
      <legend className="px-1 text-sm font-medium text-ink-800">
        {resourceLabel(group.resource)} <span className="font-mono text-xs font-normal text-ink-500">{group.resource}</span>
      </legend>

      <Checkbox
        label={text.wholeResource}
        hint={text.wholeResourceHint(wildcard)}
        checked={whole}
        disabled={!allows(callerScopes, wildcard)}
        onChange={(event) => onToggle(wildcard, event.target.checked)}
      />

      <div className="space-y-1.5 border-t border-ink-100 pt-2">
        {group.scopes.map((permission) => (
          <Checkbox
            key={permission.scope}
            label={actionLabel(permission.action)}
            hint={<span className="font-mono">{permission.scope}</span>}
            checked={whole || selected.has(permission.scope)}
            disabled={whole || !allows(callerScopes, permission.scope)}
            onChange={(event) => onToggle(permission.scope, event.target.checked)}
          />
        ))}
      </div>
    </fieldset>
  );
}

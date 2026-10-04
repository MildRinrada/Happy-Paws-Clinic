import { Checkbox } from '@/components/ui/checkbox';
import { allows } from '@/lib/auth/scopes';
import { roleSettings } from '../../content/role-settings';
import { EVERY_SCOPE } from '../../lib/scope-groups';
import type { PermissionGroup } from '../../types/role.types';
import { ScopeGroup } from './scope-group';

interface ScopePickerProps {
  groups: PermissionGroup[];
  callerScopes: string[];
  selected: string[];
  error?: string;
  onChange: (selected: string[]) => void;
}

/**
 * A role's whole scope set as checkboxes, grouped by resource. A box the caller could not grant is
 * locked: the backend refuses handing out a scope the caller does not hold.
 *
 * Wildcards are kept as the API stores them. Ticking `consult:*` drops the single `consult:` scopes
 * it covers, and ticking `*` replaces everything, so the saved set says what was meant.
 */
export function ScopePicker({ groups, callerScopes, selected, error, onChange }: ScopePickerProps) {
  const text = roleSettings.picker;
  const current = new Set(selected);
  const everything = current.has(EVERY_SCOPE);

  function toggle(scope: string, on: boolean) {
    if (scope === EVERY_SCOPE) return onChange(on ? [EVERY_SCOPE] : []);

    const next = new Set(current);

    if (on) {
      next.add(scope);
      if (scope.endsWith(':*')) {
        const prefix = scope.slice(0, -1);
        for (const value of current) if (value !== scope && value.startsWith(prefix)) next.delete(value);
      }
    } else {
      next.delete(scope);
    }

    onChange([...next]);
  }

  return (
    <fieldset className="space-y-3">
      <legend className="text-sm font-medium text-ink-700">{roleSettings.form.scopes}</legend>
      <p className="flex flex-wrap justify-between gap-x-3 text-sm text-ink-500">
        <span>{roleSettings.form.scopesHint}</span>
        <span className="font-medium text-ink-700">{text.selected(selected.length)}</span>
      </p>

      <Checkbox
        label={text.everything}
        hint={text.everythingHint}
        checked={everything}
        disabled={!allows(callerScopes, EVERY_SCOPE)}
        onChange={(event) => toggle(EVERY_SCOPE, event.target.checked)}
      />

      {everything ? (
        <p className="text-sm text-ink-500">{text.coveredByEverything}</p>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2">
          {groups.map((group) => (
            <ScopeGroup
              key={group.resource}
              group={group}
              callerScopes={callerScopes}
              selected={current}
              onToggle={toggle}
            />
          ))}
        </div>
      )}

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </fieldset>
  );
}

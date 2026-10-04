import { Checkbox } from '@/components/ui/checkbox';
import { roleLabel } from '../../content/staff-labels';
import { staffSettings } from '../../content/staff-settings';
import { canGrantAll } from '../../lib/scope-grants';
import type { Role } from '../../types/role.types';

interface RolePickerProps {
  roles: Role[];
  callerScopes: string[];
  /** Role names. */
  selected: string[];
  error?: string;
  onChange: (selected: string[]) => void;
}

/**
 * The roles someone holds. The portal role is never offered, and a role carrying a scope the caller
 * lacks is shown but locked: the backend would refuse handing it out.
 */
export function RolePicker({ roles, callerScopes, selected, error, onChange }: RolePickerProps) {
  const text = staffSettings.edit;
  const options = roles.filter((role) => role.assignable);

  function toggle(name: string, on: boolean) {
    const next = new Set(selected);
    if (on) next.add(name);
    else next.delete(name);
    onChange(options.map((role) => role.name).filter((option) => next.has(option)));
  }

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium text-ink-700">{text.roles}</legend>
      <p className="text-sm text-ink-500">{text.rolesHint}</p>
      <div className="grid gap-2 sm:grid-cols-2">
        {options.map((role) => {
          const locked = !canGrantAll(callerScopes, role.scopes);

          return (
            <Checkbox
              key={role.id}
              label={roleLabel(role.name)}
              hint={locked ? text.roleLocked : role.name}
              checked={selected.includes(role.name)}
              disabled={locked}
              className={locked ? 'opacity-60' : ''}
              onChange={(event) => toggle(role.name, event.target.checked)}
            />
          );
        })}
      </div>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </fieldset>
  );
}

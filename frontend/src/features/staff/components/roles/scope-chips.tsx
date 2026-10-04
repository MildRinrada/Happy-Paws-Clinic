import { roleSettings } from '../../content/role-settings';
import { EVERY_SCOPE } from '../../lib/scope-groups';

/** Past this many, the list folds away: `clinic_admin` alone carries most of the catalogue. */
const FOLD_AFTER = 12;

/** A role's scopes as stored, wildcards unexpanded, so `invoice:*` reads as the one grant it is. */
export function ScopeChips({ scopes }: { scopes: string[] }) {
  if (scopes.includes(EVERY_SCOPE)) {
    return <p className="text-sm font-medium text-brand-800">{roleSettings.summary.everything}</p>;
  }

  const list = (
    <ul className="flex flex-wrap gap-1.5">
      {[...scopes].sort().map((scope) => (
        <li key={scope} className="rounded-md bg-ink-100 px-2 py-0.5 font-mono text-xs text-ink-700">
          {scope}
        </li>
      ))}
    </ul>
  );

  if (scopes.length <= FOLD_AFTER) return list;

  return (
    <details className="space-y-2">
      <summary className="cursor-pointer text-sm text-ink-600">{roleSettings.summary.showScopes(scopes.length)}</summary>
      {list}
    </details>
  );
}

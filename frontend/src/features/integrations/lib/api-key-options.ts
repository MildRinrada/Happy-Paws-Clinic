import { actionLabel, resourceLabel } from '@/features/staff/content/scope-labels';
import { apiKeyScopes } from '../content/api-key-settings';

/**
 * The scopes this session may put on a key, as checkbox options: those a key may carry at all and
 * the session holds itself. The backend checks both again; this only decides what to draw.
 */
export function apiKeyScopeOptions(can: (scope: string) => boolean): Record<string, string> {
  const options: Record<string, string> = {};

  for (const scope of apiKeyScopes) {
    if (can(scope)) {
      const [resource = scope, action = ''] = scope.split(':');
      options[scope] = `${resourceLabel(resource)}: ${actionLabel(action)} (${scope})`;
    }
  }

  return options;
}

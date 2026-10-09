import { formatDateTime } from '@/lib/format/datetime';
import { apiKeyListText } from '../content/api-key-list';
import type { ApiKeySummary } from '../types/integration.types';
import { ApiKeyStatusBadge } from './api-key-status-badge';
import { RevokeApiKeyButton } from './revoke-api-key-button';

interface ApiKeyRowProps {
  apiKey: ApiKeySummary;
  /** Branch names by id, for the session's branches. */
  branchNames: Record<string, string>;
}

/** One key: what it is called, what it may do, where, and when; and the revoke button while it works. */
export function ApiKeyRow({ apiKey, branchNames }: ApiKeyRowProps) {
  const text = apiKeyListText;
  const branches =
    apiKey.branch_ids.length === 0 ? text.allBranches : apiKey.branch_ids.map((id) => branchNames[id] ?? id).join(', ');

  return (
    <tr className="align-top">
      <td className="px-4 py-3">
        <span className="block text-ink-900">{apiKey.name}</span>
        <span className="block font-mono text-xs text-ink-500">{apiKey.prefix}</span>
      </td>
      <td className="max-w-56 px-4 py-3 font-mono text-xs text-ink-700">{apiKey.scopes.join(', ')}</td>
      <td className="px-4 py-3 text-ink-700">{branches}</td>
      <td className="px-4 py-3">
        <ApiKeyStatusBadge status={apiKey.status} />
      </td>
      <td className="whitespace-nowrap px-4 py-3 text-xs tabular-nums text-ink-600">
        <span className="block">{text.created(formatDateTime(apiKey.created_at))}</span>
        <span className="block">{text.expires(formatDateTime(apiKey.expires_at))}</span>
        <span className="block">{apiKey.last_used_at ? text.lastUsed(formatDateTime(apiKey.last_used_at)) : text.neverUsed}</span>
        {apiKey.revoked_at && <span className="block text-red-700">{text.revokedAt(formatDateTime(apiKey.revoked_at))}</span>}
      </td>
      <td className="px-4 py-3">{apiKey.status === 'active' && <RevokeApiKeyButton id={apiKey.id} />}</td>
    </tr>
  );
}

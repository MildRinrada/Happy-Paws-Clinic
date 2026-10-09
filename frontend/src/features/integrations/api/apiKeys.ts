import { staffApi } from '@/lib/api/server-client';
import type { ApiKeyInput, ApiKeySummary, CreatedApiKey } from '../types/integration.types';

/** `POST /staff/api-keys` — needs `integration:manage`. The answer carries the whole key, once. */
export function createApiKey(input: ApiKeyInput): Promise<CreatedApiKey> {
  return staffApi().post<CreatedApiKey>('/staff/api-keys', { body: input });
}

/**
 * `GET /staff/api-keys` — the keys this session could have made, newest first, revoked and expired
 * ones included. Never the key. A contract extension (docs/contract-extensions.md § 22).
 */
export async function listApiKeys(): Promise<ApiKeySummary[]> {
  return (await staffApi().get<{ data: ApiKeySummary[] }>('/staff/api-keys')).data;
}

/** `DELETE /staff/api-keys/{id}` — the key's next request is refused. docs/contract-extensions.md § 22. */
export function revokeApiKey(id: string): Promise<void> {
  return staffApi().delete<void>(`/staff/api-keys/${encodeURIComponent(id)}`);
}

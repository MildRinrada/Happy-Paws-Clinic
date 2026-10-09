import { staffApi } from '@/lib/api/server-client';
import type { ApiKeyInput, CreatedApiKey } from '../types/integration.types';

/** `POST /staff/api-keys` — needs `integration:manage`. The answer carries the whole key, once. */
export function createApiKey(input: ApiKeyInput): Promise<CreatedApiKey> {
  return staffApi().post<CreatedApiKey>('/staff/api-keys', { body: input });
}

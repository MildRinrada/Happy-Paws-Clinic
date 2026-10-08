import { staffApi } from '@/lib/api/server-client';

/**
 * `DELETE /staff/automations/{id}` (automation:manage), a contract extension
 * (docs/contract-extensions.md § 19). The rule stops and leaves the list; what it sent stays in the history.
 */
export function deleteAutomation(id: string, version: number): Promise<void> {
  return staffApi().delete<void>(`/staff/automations/${encodeURIComponent(id)}`, { ifMatch: version });
}

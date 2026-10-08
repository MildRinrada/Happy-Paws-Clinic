import { staffApi } from '@/lib/api/server-client';
import type { Automation, UpdateAutomationInput } from '../types/automation.types';

/**
 * `PATCH /staff/automations/{id}` (automation:manage), a contract extension
 * (docs/contract-extensions.md § 19): switch a rule on or off, or edit it. Answers with the rule.
 */
export function updateAutomation(id: string, version: number, input: UpdateAutomationInput): Promise<Automation> {
  return staffApi().patch<Automation>(`/staff/automations/${encodeURIComponent(id)}`, { body: input, ifMatch: version });
}

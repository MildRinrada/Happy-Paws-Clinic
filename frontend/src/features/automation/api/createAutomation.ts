import { staffApi } from '@/lib/api/server-client';
import type { Automation, CreateAutomationInput } from '../types/automation.types';

/** `POST /staff/automations` (automation:manage). Answers 201 with the rule. */
export function createAutomation(input: CreateAutomationInput): Promise<Automation> {
  return staffApi().post<Automation>('/staff/automations', { body: input });
}

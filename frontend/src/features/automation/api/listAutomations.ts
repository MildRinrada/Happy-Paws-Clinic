import { staffApi } from '@/lib/api/server-client';
import type { Automation } from '../types/automation.types';

/** `GET /staff/automations` (automation:read). Every rule the session may see, with its history. */
export async function listAutomations(): Promise<Automation[]> {
  return (await staffApi().get<{ data: Automation[] }>('/staff/automations')).data;
}

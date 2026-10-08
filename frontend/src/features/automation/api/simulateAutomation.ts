import { staffApi } from '@/lib/api/server-client';
import type { Simulation } from '../types/automation.types';

/**
 * `POST /staff/automations/{id}/simulate` (automation:manage). Who the rule would message over the
 * next `days`, worked out by the same code as the real run. Nothing is sent.
 */
export function simulateAutomation(id: string, days: number): Promise<Simulation> {
  return staffApi().post<Simulation>(`/staff/automations/${encodeURIComponent(id)}/simulate`, { body: { days } });
}

import { staffApi } from '@/lib/api/server-client';
import type { LineSettings, LineSettingsInput } from '../types/line.types';

/**
 * `PUT /staff/integrations/line` — "channel id/secret · เปิดปิดชนิดการแจ้งเตือน". Needs
 * `integration:manage`. The contract has no `GET`, so an empty body is how the settings are read:
 * it writes nothing and returns them as they stand.
 */
export function readLineSettings(): Promise<LineSettings> {
  return staffApi().put<LineSettings>('/staff/integrations/line', { body: {} });
}

/** Changes the fields given and leaves the rest. A stale `version` answers `409 version_conflict`. */
export function configureLine(version: number, input: LineSettingsInput): Promise<LineSettings> {
  return staffApi().put<LineSettings>('/staff/integrations/line', { body: input, ifMatch: version });
}

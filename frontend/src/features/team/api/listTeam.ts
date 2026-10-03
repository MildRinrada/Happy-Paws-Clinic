import { publicApi } from '@/lib/api/server-client';
import type { DepartmentCode, TeamDepartment } from '../types/team.types';

/**
 * `GET /team?department=` — the clinic's people grouped by department. Public.
 *
 * Not in the API contract; it is a contract extension (docs/contract-extensions.md § 10).
 */
export async function listTeam(department?: DepartmentCode): Promise<TeamDepartment[]> {
  const { data } = await publicApi().get<{ data: TeamDepartment[] }>('/team', {
    query: { department },
  });

  return data;
}

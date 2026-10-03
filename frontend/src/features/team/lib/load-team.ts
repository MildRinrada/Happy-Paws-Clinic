import { listTeam } from '../api/listTeam';
import type { DepartmentCode, TeamDepartment, TeamMember } from '../types/team.types';

/**
 * The team for a public page, or `null` if the API cannot be reached.
 *
 * Like the articles column: the page still renders when the API is away, and says so where the
 * team would have been.
 */
export async function loadTeam(department?: DepartmentCode): Promise<TeamDepartment[] | null> {
  try {
    return await listTeam(department);
  } catch {
    return null;
  }
}

/** The first `count` people of one department, or `null` if the API cannot be reached. */
export async function loadDepartmentMembers(department: DepartmentCode, count: number): Promise<TeamMember[] | null> {
  const team = await loadTeam(department);

  return team === null ? null : (team[0]?.members ?? []).slice(0, count);
}

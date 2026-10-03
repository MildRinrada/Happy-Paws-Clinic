import { listServices } from '@/features/service-catalog/api/listServices';
import { listTeam } from '@/features/team/api/listTeam';

/**
 * Names for the ids an appointment carries, from the two public lists that have them.
 *
 * Both lists are partial on purpose: `GET /services` holds only services open for online booking,
 * and `GET /team` only people the clinic shows on its site. A miss is normal and the caller falls
 * back to a general word. A failure to read either list is treated the same way, so a name never
 * costs the owner the page.
 */

async function quietly<T>(read: () => Promise<T>, fallback: T): Promise<T> {
  try {
    return await read();
  } catch {
    return fallback;
  }
}

/** Service id to name, across the branches given. */
export async function serviceNames(branchIds: string[]): Promise<Map<string, string>> {
  const lists = await Promise.all([...new Set(branchIds)].map((branch) => quietly(() => listServices(branch), [])));

  return new Map(lists.flat().map((service) => [service.id, service.name]));
}

/** Staff id to name with title ("สพ.ญ. มินท์ ศรีสุข"), for the vets on the public team page. */
export async function vetNames(): Promise<Map<string, string>> {
  const departments = await quietly(() => listTeam('veterinary'), []);

  return new Map(departments.flatMap((department) => department.members).map((member) => [member.id, member.display_name]));
}

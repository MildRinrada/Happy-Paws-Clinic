/**
 * Whether a pet answers to what was typed in a search box: part of its name or nickname, ignoring
 * case and spaces at either end. An empty box matches every pet.
 */
export function matchesPet(pet: { name: string; nickname?: string | null }, query: string): boolean {
  const wanted = query.trim().toLowerCase();
  if (!wanted) return true;

  return [pet.name, pet.nickname].some((value) => value?.toLowerCase().includes(wanted));
}

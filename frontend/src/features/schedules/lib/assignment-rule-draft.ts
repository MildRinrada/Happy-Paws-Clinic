import type { AssignmentRule } from '../types/schedule.types';

/** The backend's limits (`AssignmentRules`): 50 rules, 1 to 20 people each, species up to 40 letters. */
export const MAX_RULES = 50;
export const MAX_STAFF_PER_RULE = 20;
export const SPECIES_MAX_LENGTH = 40;

/** One rule as the form edits it. `key` keeps React's rows steady while rules move up and down. */
export interface RuleDraft {
  key: number;
  species: string;
  serviceId: string;
  staffIds: string[];
}

export type RuleProblem = 'needMatch' | 'needStaff' | 'tooManyStaff';

let nextKey = 0;

export function emptyRule(species = ''): RuleDraft {
  return { key: nextKey++, species, serviceId: '', staffIds: [] };
}

/**
 * Where the form starts when no rules are stored yet: the contract's own example,
 * "หมอแมวรับเคสแมวก่อน", with the vet still to be chosen.
 */
export function startingRules(): RuleDraft[] {
  return [emptyRule('แมว')];
}

/** The rules the backend answered with, back into the form. */
export function draftsFrom(rules: AssignmentRule[]): RuleDraft[] {
  return rules.map((rule) => ({
    key: nextKey++,
    species: rule.species ?? '',
    serviceId: rule.service_id ?? '',
    staffIds: [...rule.staff_ids],
  }));
}

/** What is wrong with a rule before it is sent, or `undefined`. The backend checks again. */
export function ruleProblem(rule: RuleDraft): RuleProblem | undefined {
  if (!rule.species.trim() && !rule.serviceId) return 'needMatch';
  if (rule.staffIds.length === 0) return 'needStaff';
  if (rule.staffIds.length > MAX_STAFF_PER_RULE) return 'tooManyStaff';
  return undefined;
}

export function toRequest(rule: RuleDraft): { species?: string; service_id?: string; staff_ids: string[] } {
  return {
    species: rule.species.trim() || undefined,
    service_id: rule.serviceId || undefined,
    staff_ids: rule.staffIds,
  };
}

/** The list with the item at `from` moved to `to`. Out-of-range moves leave it as it was. */
export function moved<T>(items: T[], from: number, to: number): T[] {
  if (to < 0 || to >= items.length) return items;

  const copy = [...items];
  const [item] = copy.splice(from, 1);
  copy.splice(to, 0, item!);

  return copy;
}

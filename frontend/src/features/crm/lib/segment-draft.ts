import { parseBahtToSatang } from '@/lib/format/money';
import type { LoyaltyTier, NewSegmentInput, SegmentCriteriaInput } from '../types/crm.types';
import { parseCount } from './parse-count';

export type PackageFilter = 'any' | 'yes' | 'no';

/** A segment's conditions as typed. An empty box is not a condition. */
export interface SegmentDraft {
  name: string;
  species: string[];
  inactiveDays: string;
  visitedWithinDays: string;
  minSpend: string;
  tiers: LoyaltyTier[];
  activePackage: PackageFilter;
  /** '' for every branch. */
  branchId: string;
}

export type SegmentProblem = 'name' | 'inactiveDays' | 'visitedWithinDays' | 'minSpend' | 'noCriteria';

export const MAX_SPECIES = 10;

export const emptySegmentDraft = (): SegmentDraft => ({
  name: '',
  species: [],
  inactiveDays: '',
  visitedWithinDays: '',
  minSpend: '',
  tiers: [],
  activePackage: 'any',
  branchId: '',
});

/** A day-count box: `null` when empty, `undefined` when filled with something that is not 1 to 3650. */
function parseDays(input: string): number | null | undefined {
  if (!input.trim()) return null;
  const value = parseCount(input);

  return value !== null && value >= 1 && value <= 3650 ? value : undefined;
}

function parseSpend(input: string): number | null | undefined {
  if (!input.trim()) return null;
  const value = parseBahtToSatang(input);

  return value !== null && value > 0 ? value : undefined;
}

/** The conditions as the API wants them. Only the filled ones; the branch only when one is picked. */
function toCriteria(draft: SegmentDraft): SegmentCriteriaInput {
  const inactive = parseDays(draft.inactiveDays);
  const within = parseDays(draft.visitedWithinDays);
  const spend = parseSpend(draft.minSpend);

  return {
    ...(draft.species.length > 0 && { species: draft.species }),
    ...(typeof inactive === 'number' && { inactive_days: inactive }),
    ...(typeof within === 'number' && { visited_within_days: within }),
    ...(typeof spend === 'number' && { min_spend_satang: spend }),
    ...(draft.tiers.length > 0 && { tiers: draft.tiers }),
    ...(draft.activePackage !== 'any' && { has_active_package: draft.activePackage === 'yes' }),
  };
}

/** Which parts break the API's own rules. The backend checks them again. */
export function segmentProblems(draft: SegmentDraft): SegmentProblem[] {
  const problems: SegmentProblem[] = [];
  const inactive = parseDays(draft.inactiveDays);
  const within = parseDays(draft.visitedWithinDays);

  if (!draft.name.trim()) problems.push('name');
  if (inactive === undefined) problems.push('inactiveDays');
  if (within === undefined || (typeof within === 'number' && typeof inactive === 'number' && within <= inactive)) {
    problems.push('visitedWithinDays');
  }
  if (parseSpend(draft.minSpend) === undefined) problems.push('minSpend');
  if (Object.keys(toCriteria(draft)).length === 0 && !draft.branchId) problems.push('noCriteria');

  return problems;
}

/** The request body, once `segmentProblems` is empty. */
export function toSegmentInput(draft: SegmentDraft): NewSegmentInput {
  return {
    name: draft.name.trim(),
    criteria: { ...toCriteria(draft), ...(draft.branchId && { branch_id: draft.branchId }) },
  };
}

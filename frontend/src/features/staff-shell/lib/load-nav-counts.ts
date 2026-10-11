import type { StaffContext } from '@/features/account/lib/staff-context';
import { tolerant } from '@/features/analytics/lib/tolerant-read';
import { Scope } from '@/lib/auth/scopes';
import type { NavBadgeKey, NavCount, NavCounts } from '../types/staff-shell.types';
import { labWork, stockAlerts, unansweredThreads, waitingTickets } from './nav-count-probes';

type Probe = [NavBadgeKey, () => Promise<NavCount | null>];

/**
 * The menu's badges, read side by side. A badge is asked for only with the scope its own page needs
 * (the same gates as the overview's to-do box), and only for the branch in view. A read that is
 * refused or fails leaves its badge out, and so does a count of zero: the menu never shows a number
 * it could not read.
 */
export async function loadNavCounts(context: StaffContext): Promise<NavCounts> {
  const branchId = context.branch?.id;
  const probes: Probe[] = [];

  if (branchId && context.can(Scope.QueueRead)) probes.push(['queue', () => waitingTickets(branchId)]);
  if (context.can(Scope.MessageRead)) probes.push(['messages', unansweredThreads]);
  if (branchId && context.can(Scope.InventoryRead)) probes.push(['stock', () => stockAlerts(branchId)]);
  // Reading the list takes lab:read, as the page does; lab:write decides whose work is counted.
  if (branchId && context.can(Scope.LabRead)) probes.push(['lab', () => labWork(branchId, context.can(Scope.LabWrite))]);

  const answers = await Promise.all(probes.map(async ([key, probe]) => ({ key, count: await tolerant(probe) })));
  const counts: NavCounts = {};

  for (const { key, count } of answers) {
    if (count) counts[key] = count;
  }

  return counts;
}

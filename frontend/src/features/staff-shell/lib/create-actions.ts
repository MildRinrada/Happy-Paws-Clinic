import { quickActionText } from '@/features/analytics/content/quick-actions';
import { quickActions, type QuickActionKey } from '@/features/analytics/lib/quick-actions';
import type { CreateAction } from '../types/staff-shell.types';

/** The order the "+ สร้าง" menu lists the jobs in: the commonest at the counter first. */
const ORDER: QuickActionKey[] = ['book', 'issueTicket', 'register', 'collect'];

/**
 * The overview's shortcuts again, as the top bar's "+ สร้าง" menu. Each one is gated exactly as on
 * the overview: shown only when the session holds every scope its page needs. Empty when none
 * apply, and the button is then not drawn.
 */
export function createActions(can: (scope: string) => boolean): CreateAction[] {
  return ORDER.flatMap((key) => {
    const action = quickActions.find((candidate) => candidate.key === key);

    return action && action.scopes.every(can) ? [{ key, href: action.href, label: quickActionText[key] }] : [];
  });
}

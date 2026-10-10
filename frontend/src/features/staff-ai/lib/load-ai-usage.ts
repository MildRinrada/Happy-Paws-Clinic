import { unstable_rethrow } from 'next/navigation';
import { cache } from 'react';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { getUsage } from '../api/getUsage';
import type { AiUsage } from '../types/staff-ai.types';

/**
 * This session's view of the AI assistant, once per request: the month's usage when the session
 * holds `ai:use` and the assistant is switched on, otherwise null. Every AI control asks this and
 * draws nothing on null, so a deployment without a model, or a role without the scope, sees no
 * AI anywhere. An API that cannot be reached counts as off.
 */
export const loadAiUsage = cache(async (): Promise<AiUsage | null> => {
  const context = await loadStaffContext();

  if (!context?.can(Scope.AiUse)) {
    return null;
  }

  try {
    const usage = await authed(() => optional(getUsage()));
    return usage?.enabled ? usage : null;
  } catch (error) {
    // A lapsed session still goes to sign in; anything else just means no AI on this page.
    unstable_rethrow(error);
    return null;
  }
});

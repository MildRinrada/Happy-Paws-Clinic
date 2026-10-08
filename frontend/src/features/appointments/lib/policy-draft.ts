import { parseBahtToSatang } from '@/lib/format/money';
import type { CancellationPolicy, CancellationPolicyInput } from '../types/appointment-settings.types';

/** The policy as typed. Strings, so a half-typed number is not forced into something else. */
export interface PolicyDraft {
  cutoffHours: string;
  feeBaht: string;
  freeAllowance: string;
  windowDays: string;
}

export type PolicyField = keyof PolicyDraft;

/** The backend's defaults (`CancellationPolicy`): 24 hours, no fee, one forgiven, a year back. */
export function defaultPolicy(): PolicyDraft {
  return { cutoffHours: '24', feeBaht: '0', freeAllowance: '1', windowDays: '365' };
}

export function draftFrom(policy: CancellationPolicy): PolicyDraft {
  return {
    cutoffHours: String(policy.cutoff_hours),
    feeBaht: String(policy.fee_satang / 100),
    freeAllowance: String(policy.free_allowance),
    windowDays: String(policy.window_days),
  };
}

function whole(text: string): number | null {
  return /^\d{1,6}$/.test(text.trim()) ? Number(text.trim()) : null;
}

/**
 * The request body, or the fields that are not numbers at all. Whether a number is in range is the
 * backend's rule and its refusal says which.
 */
export function toPolicyInput(draft: PolicyDraft): { input?: CancellationPolicyInput; bad: PolicyField[] } {
  const values = {
    cutoff_hours: whole(draft.cutoffHours),
    fee_satang: parseBahtToSatang(draft.feeBaht),
    free_allowance: whole(draft.freeAllowance),
    window_days: whole(draft.windowDays),
  };

  const bad: PolicyField[] = [];
  if (values.cutoff_hours === null) bad.push('cutoffHours');
  if (values.fee_satang === null) bad.push('feeBaht');
  if (values.free_allowance === null) bad.push('freeAllowance');
  if (values.window_days === null) bad.push('windowDays');

  return bad.length > 0 ? { bad } : { input: values as CancellationPolicyInput, bad };
}

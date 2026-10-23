import { users } from '../../core/auth';
import { nowIso } from '../../core/db';
import type { Ctx } from '../../core/http';
import { auditLogs, staffMembers } from './data';

/** Drops absent and empty optional fields, as the API leaves them out of an answer. */
export function compact<T extends object>(value: T): T {
  return Object.fromEntries(Object.entries(value).filter(([, field]) => field !== undefined && field !== null && field !== '')) as T;
}

/** An optional text field of a full-replace body: an empty string clears it. */
export const optionalText = (value: unknown): string | undefined => {
  const text = typeof value === 'string' ? value.trim() : '';
  return text || undefined;
};

/** Writes one line of the audit trail for a change made through these endpoints. */
export function audit(ctx: Ctx, action: string, resource_type: string, summary: string, resource_id?: string): void {
  const staff = ctx.auth?.staffId ? staffMembers().get(ctx.auth.staffId) : undefined;
  const actorName = staff?.display_name ?? users().get(ctx.auth?.userId)?.display_name ?? 'ระบบ';
  auditLogs().insert({
    occurred_at: nowIso(),
    actor_id: staff?.id,
    actor_name: actorName,
    action,
    resource_type,
    resource_id,
    summary,
    ip: '203.150.12.44',
  });
}

export const isDate = (value: unknown): value is string => typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value);
export const isTime = (value: unknown): value is string => typeof value === 'string' && /^([01]\d|2[0-3]):[0-5]\d$/.test(value);
export const isInstant = (value: unknown): value is string => typeof value === 'string' && !Number.isNaN(Date.parse(value)) && value.includes('T');

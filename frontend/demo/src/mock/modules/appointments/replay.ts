import type { Ctx } from '../../core/http';
import { replays } from './store';

/** The same idempotency key answers the first result again instead of booking twice. */
export function once<T extends { id: string }>(ctx: Ctx, scope: string, make: () => T, find: (id: string) => T | undefined): T {
  const key = ctx.idempotencyKey;
  if (key) {
    const seen = replays().find((row) => row.key === key && row.scope === scope);
    const earlier = seen ? find(seen.result_id) : undefined;
    if (earlier) return earlier;
  }
  const result = make();
  if (key) replays().insert({ key, scope, result_id: result.id });
  return result;
}

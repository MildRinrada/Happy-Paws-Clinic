import { redirect } from 'next/navigation';
import { ApiError } from '@/lib/api/api-error';
import { SIGN_IN_PATH } from '@/lib/auth/session';

/**
 * What a server action hands back to the form that called it.
 *
 * A refusal from the backend is an outcome, not an exception: "ช่วงเวลานี้ถูกจองแล้ว" or "ยอดชำระ
 * เกินยอดค้าง" is something the person at the counter reads and acts on. So it travels as data —
 * the backend's own Thai message and the field it names — and only a genuine fault is thrown.
 */
export type ActionResult<T = undefined> =
  | { ok: true; data: T }
  | { ok: false; error: ActionError };

export interface ActionError {
  /** The contract's `error.code`. Branch on this, never on the message. */
  code: string;
  message: string;
  field?: string;
  status: number;
  /** The envelope's `details`, e.g. `suggested` times on `slot_unavailable`. */
  details?: Record<string, unknown>;
}

/**
 * Runs a backend mutation and folds its refusal into a result.
 *
 * A 401 is the one refusal not reported to the form: the session is over, so the visitor is sent
 * to sign in. Everything that is not an `ApiError` — including the redirect itself — is rethrown.
 */
export async function runAction<T>(mutation: () => Promise<T>): Promise<ActionResult<T>> {
  try {
    return { ok: true, data: await mutation() };
  } catch (error) {
    if (!(error instanceof ApiError)) {
      throw error;
    }

    if (error.status === 401) {
      redirect(SIGN_IN_PATH);
    }

    return {
      ok: false,
      error: { code: error.code, message: error.message, field: error.field, status: error.status, details: error.details },
    };
  }
}

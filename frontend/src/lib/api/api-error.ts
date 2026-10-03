/**
 * The backend's shared error envelope, typed once for the whole app.
 * Every endpoint returns this shape, so features switch on `code` instead of parsing messages.
 */
export interface ApiErrorBody {
  code: string;
  message: string;
  message_en?: string;
  field?: string;
  request_id: string;
  details?: Record<string, unknown>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly field?: string;
  readonly requestId: string;
  readonly details?: Record<string, unknown>;

  constructor(status: number, body: ApiErrorBody) {
    super(body.message);
    this.name = 'ApiError';
    this.status = status;
    this.code = body.code;
    this.field = body.field;
    this.requestId = body.request_id;
    this.details = body.details;
  }
}

/** The codes the API reference names explicitly; features may branch on these safely. */
export const ApiErrorCode = {
  Unauthenticated: 'unauthenticated',
  InsufficientScope: 'insufficient_scope',
  StateConflict: 'state_conflict',
  VersionConflict: 'version_conflict',
  RecordLocked: 'record_locked',
  SlotUnavailable: 'slot_unavailable',
} as const;

/**
 * Runs a read that the page can do without, answering `null` when the caller lacks the scope for
 * it or the row is gone.
 *
 * A queue board shows a ticket whether or not this session may read the patient behind it — the
 * backend decides that, per scope, and the screen degrades to an id rather than failing whole.
 * Anything else — a 500, a dropped connection, an expired session — is not "optional data" and is
 * rethrown.
 */
export async function optional<T>(read: Promise<T>): Promise<T | null> {
  try {
    return await read;
  } catch (error) {
    if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
      return null;
    }
    throw error;
  }
}

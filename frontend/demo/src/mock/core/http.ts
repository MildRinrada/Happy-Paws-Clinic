/**
 * The demo's backend, as a table of routes answered inside the page. Each module under
 * mock/modules registers its own with `route()`; `dispatch()` is what the fetch shim calls.
 *
 * Paths are written the way the backend's controllers write them, without the `/v1` prefix:
 * `route('GET', '/staff/customers/:id', handler)`. Bodies and answers are the contract's
 * snake_case JSON, exactly what the product's types in src/features/<name>/types expect.
 */

export interface Auth {
  token: string;
  userId: string;
  staffId?: string;
  customerId?: string;
  /** Effective scopes of the staff membership; `['portal']` for a customer-only session. */
  scopes: string[];
  /** Branches the staff membership is limited to; empty means every branch. */
  branches: string[];
  /** A waiting-room screen's token rather than a person's. */
  display?: { branchId: string; screenId: string };
}

export interface Ctx {
  method: string;
  /** The path after `/v1`, without the query string. */
  path: string;
  params: Record<string, string>;
  query: URLSearchParams;
  /** Parsed JSON body, `FormData` for a multipart upload, or `undefined`. */
  body: any;
  headers: Headers;
  /** The signed-in caller, or `null` for an anonymous call. */
  auth: Auth | null;
  /** The `If-Match` version as a number, when one was sent. */
  ifMatch?: number;
  idempotencyKey?: string;
  /** Cookies the request carried in a `Cookie` header (the refresh token travels this way). */
  cookies: Record<string, string>;
}

/** What a handler may answer with besides a plain JSON value. */
export class Reply {
  constructor(
    readonly status: number,
    readonly body: unknown,
    readonly headers: Record<string, string> = {},
    readonly setCookies: string[] = [],
  ) {}
}

export class HttpError extends Error {
  constructor(
    readonly status: number,
    readonly code: string,
    message: string,
    readonly extra: { field?: string; details?: Record<string, unknown>; message_en?: string } = {},
  ) {
    super(message);
  }
}

export type Handler = (ctx: Ctx) => unknown | Promise<unknown>;

interface Entry {
  method: string;
  pattern: string;
  regex: RegExp;
  keys: string[];
  handler: Handler;
  /** Literal segments count; more literal wins when two patterns match. */
  weight: number;
}

const entries: Entry[] = [];

export function route(method: string, pattern: string, handler: Handler): void {
  const keys: string[] = [];
  const parts = pattern.split('/').filter(Boolean);
  const regex = new RegExp(
    `^/${parts
      .map((part) => {
        if (part.startsWith(':') || (part.startsWith('{') && part.endsWith('}'))) {
          keys.push(part.replace(/^:|^\{|\}$/g, ''));
          return '([^/]+)';
        }
        return part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
      })
      .join('/')}/?$`,
  );
  const weight = parts.filter((part) => !part.startsWith(':') && !part.startsWith('{')).length;
  entries.push({ method: method.toUpperCase(), pattern, regex, keys, handler, weight });
  entries.sort((a, b) => b.weight - a.weight);
}

export function hasRoute(method: string, path: string): boolean {
  return entries.some((entry) => entry.method === method && entry.regex.test(path));
}

export function find(method: string, path: string): { handler: Handler; params: Record<string, string> } | null {
  for (const entry of entries) {
    if (entry.method !== method) continue;
    const found = entry.regex.exec(path);
    if (!found) continue;
    const params: Record<string, string> = {};
    entry.keys.forEach((key, index) => {
      params[key] = decodeURIComponent(found[index + 1]!);
    });
    return { handler: entry.handler, params };
  }
  return null;
}

/** Every registered route, for the route-check script and the guide. */
export function listRoutes(): string[] {
  return entries.map((entry) => `${entry.method} ${entry.pattern}`);
}

// ---- Answers -------------------------------------------------------------------------------------

export const ok = (body: unknown, headers?: Record<string, string>) => new Reply(200, body, headers);
export const created = (body: unknown) => new Reply(201, body);
export const accepted = (body: unknown) => new Reply(202, body);
export const noContent = () => new Reply(204, undefined);

/** A file answer, such as a PDF the product would generate. */
export function file(content: BlobPart, type: string, name: string): Reply {
  return new Reply(200, new Blob([content], { type }), {
    'Content-Type': type,
    'Content-Disposition': `attachment; filename="${encodeURIComponent(name)}"`,
  });
}

export function fail(
  status: number,
  code: string,
  message: string,
  extra?: { field?: string; details?: Record<string, unknown>; message_en?: string },
): never {
  throw new HttpError(status, code, message, extra);
}

export function notFound(what = 'รายการ'): never {
  fail(404, 'not_found', `ไม่พบ${what}`);
}

export function invalid(field: string, message: string): never {
  fail(422, 'validation_failed', message, { field, details: { errors: { [field]: [message] } } });
}

export function conflict(message = 'ข้อมูลถูกแก้ไขโดยผู้อื่น โปรดโหลดใหม่'): never {
  fail(409, 'version_conflict', message);
}

export function stateConflict(message: string): never {
  fail(409, 'state_conflict', message);
}

// ---- Guards --------------------------------------------------------------------------------------

export function allows(scopes: string[], scope: string): boolean {
  const [area] = scope.split(':');
  return scopes.includes('*') || scopes.includes(scope) || scopes.includes(`${area}:*`);
}

export function requireAuth(ctx: Ctx): Auth {
  if (!ctx.auth) fail(401, 'unauthenticated', 'กรุณาเข้าสู่ระบบ');
  return ctx.auth;
}

/** A staff session, optionally with a scope (`'customers:read'`). */
export function requireStaff(ctx: Ctx, scope?: string): Auth & { staffId: string } {
  const auth = requireAuth(ctx);
  if (!auth.staffId) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่ใช่บัญชีพนักงาน');
  if (scope && !allows(auth.scopes, scope)) fail(403, 'insufficient_scope', 'บัญชีนี้ไม่มีสิทธิ์ทำรายการนี้');
  return auth as Auth & { staffId: string };
}

export function requireCustomer(ctx: Ctx): Auth & { customerId: string } {
  const auth = requireAuth(ctx);
  if (!auth.customerId) fail(403, 'insufficient_scope', 'บัญชีนี้ยังไม่ได้เป็นลูกค้าของคลินิก');
  return auth as Auth & { customerId: string };
}

/** Checks `If-Match` against a row's version, when the caller sent one. */
export function checkVersion(ctx: Ctx, row: { version?: number }): void {
  if (ctx.ifMatch !== undefined && row.version !== undefined && ctx.ifMatch !== row.version) conflict();
}

// ---- Lists ---------------------------------------------------------------------------------------

/**
 * The contract's cursor page. The cursor is an offset, which is all an in-memory list needs; the
 * product only ever passes back what it was given.
 */
export function page<T>(rows: T[], query: URLSearchParams, defaultLimit = 25): { data: T[]; page: { next_cursor?: string } } {
  const limit = Math.max(1, Math.min(200, Number(query.get('limit')) || defaultLimit));
  const offset = Math.max(0, Number(query.get('cursor')) || 0);
  const data = rows.slice(offset, offset + limit);
  return offset + limit < rows.length ? { data, page: { next_cursor: String(offset + limit) } } : { data, page: {} };
}

/** Case- and space-insensitive "contains", over the fields a search box looks at. */
export function matches(query: string | null | undefined, ...fields: Array<string | null | undefined>): boolean {
  const needle = (query ?? '').trim().toLowerCase().replace(/[\s-]/g, '');
  if (!needle) return true;
  return fields.some((field) => (field ?? '').toLowerCase().replace(/[\s-]/g, '').includes(needle));
}

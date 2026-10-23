import { MOCK_API } from '../../mock-api';
import { resolveToken } from './core/auth';
import { on } from './core/bus';
import { find, HttpError, Reply, type Ctx } from './core/http';
import { putUpload, readUpload } from './core/files';
import { getCookie } from '../runtime/cookies';

/**
 * Puts the demo's backend between the product and the network. Calls to the API address are
 * answered by the routes in mock/modules; everything else (fonts, the page's own files) goes out
 * as usual. `EventSource` is replaced the same way for the two queue streams.
 */

const API = new URL(MOCK_API);
const API_PREFIX = API.pathname.replace(/\/$/, '');
const STORAGE_PREFIX = '/storage/';

/** How long an answer takes, so loading states show as they would against a real server. */
const LATENCY_MS = 60;

const realFetch = window.fetch.bind(window);

function envelope(status: number, code: string, message: string, extra: object = {}) {
  return {
    error: { code, message, request_id: `demo-${Math.random().toString(36).slice(2, 10)}`, ...extra },
  };
}

function json(status: number, body: unknown, headers: Record<string, string> = {}): Response {
  return new Response(status === 204 || body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json', ...headers },
  });
}

/** `Set-Cookie` cannot be put on a Response made in a page, so the reader the product uses is given it. */
function withSetCookies(response: Response, cookies: string[]): Response {
  if (cookies.length) {
    Object.defineProperty(response.headers, 'getSetCookie', { value: () => cookies.slice() });
  }
  return response;
}

function parseIfMatch(value: string | null): number | undefined {
  if (!value) return undefined;
  const number = Number(value.replace(/^W\//, '').replace(/"/g, ''));
  return Number.isFinite(number) ? number : undefined;
}

function parseCookies(header: string | null): Record<string, string> {
  const out: Record<string, string> = {};
  for (const pair of (header ?? '').split(';')) {
    const index = pair.indexOf('=');
    if (index > 0) out[pair.slice(0, index).trim()] = decodeURIComponent(pair.slice(index + 1).trim());
  }
  return out;
}

async function readBody(request: Request): Promise<unknown> {
  if (request.method === 'GET' || request.method === 'HEAD') return undefined;
  const type = request.headers.get('content-type') ?? '';
  if (type.includes('multipart/form-data')) return request.formData();
  const text = await request.text();
  if (!text) return undefined;
  if (type.includes('json') || /^[[{]/.test(text.trim())) {
    try {
      return JSON.parse(text);
    } catch {
      return text;
    }
  }
  return text;
}

export async function dispatch(request: Request): Promise<Response> {
  const url = new URL(request.url);
  await new Promise((resolve) => setTimeout(resolve, LATENCY_MS));

  if (url.pathname.startsWith(STORAGE_PREFIX)) {
    const id = url.pathname.slice(STORAGE_PREFIX.length);
    if (request.method === 'PUT') {
      await putUpload(id, await request.blob());
      return new Response(null, { status: 204 });
    }
    const stored = await readUpload(id);
    return stored ? new Response(stored) : new Response(null, { status: 404 });
  }

  const path = url.pathname.slice(API_PREFIX.length) || '/';
  const method = request.method.toUpperCase();
  const found = find(method, path);

  if (!found) {
    console.warn(`[mock] unhandled ${method} ${path}`);
    return json(404, envelope(404, 'not_found', 'เดโมยังไม่รองรับรายการนี้', { message_en: `Not in the demo: ${method} ${path}` }));
  }

  const bearer = request.headers.get('authorization')?.replace(/^Bearer\s+/i, '');
  const ctx: Ctx = {
    method,
    path,
    params: found.params,
    query: url.searchParams,
    body: await readBody(request),
    headers: request.headers,
    auth: bearer ? resolveToken(bearer) : null,
    ifMatch: parseIfMatch(request.headers.get('if-match')),
    idempotencyKey: request.headers.get('idempotency-key') ?? undefined,
    cookies: parseCookies(request.headers.get('cookie')),
  };

  try {
    const result = await found.handler(ctx);
    if (result instanceof Reply) {
      if (result.body instanceof Blob) {
        return new Response(result.body, { status: result.status, headers: result.headers });
      }
      return withSetCookies(json(result.status, result.body, result.headers), result.setCookies);
    }
    return result === undefined ? json(204, undefined) : json(200, result);
  } catch (error) {
    if (error instanceof HttpError) {
      return json(error.status, envelope(error.status, error.code, error.message, error.extra));
    }
    console.error(`[mock] ${method} ${path} failed`, error);
    return json(500, envelope(500, 'internal_error', 'เดโมทำรายการนี้ไม่สำเร็จ'));
  }
}

function isApi(url: URL): boolean {
  return url.origin === API.origin;
}

// ---- fetch -----------------------------------------------------------------------------------------

window.fetch = async (input: RequestInfo | URL, init?: RequestInit) => {
  const url = new URL(input instanceof Request ? input.url : String(input), location.href);
  if (!isApi(url)) return realFetch(input, init);

  // `next` and `cache` are Next's own fetch options; a Request in the page has no use for them.
  const { next: _next, ...rest } = (init ?? {}) as RequestInit & { next?: unknown };
  return dispatch(new Request(input instanceof Request ? input : url, rest));
};

// ---- EventSource -----------------------------------------------------------------------------------

/**
 * The two streams the product opens from the browser, `/api/queue/stream` (the console) and
 * `/display/stream` (a screen with its own token), both answered here. Any change to a branch's
 * queue arrives as `ticket.updated`; the product reads the board again on any event.
 */
const STREAM_PATHS = ['/api/queue/stream', '/display/stream'];
const RealEventSource = window.EventSource;

class DemoEventSource extends EventTarget {
  static readonly CONNECTING = 0;
  static readonly OPEN = 1;
  static readonly CLOSED = 2;
  readonly CONNECTING = 0;
  readonly OPEN = 1;
  readonly CLOSED = 2;

  readonly url: string;
  readonly withCredentials = false;
  readyState = 0;
  onopen: ((event: Event) => void) | null = null;
  onmessage: ((event: MessageEvent) => void) | null = null;
  onerror: ((event: Event) => void) | null = null;
  private off: (() => void) | undefined;

  constructor(url: string | URL) {
    super();
    const target = new URL(String(url), location.href);
    this.url = target.href;
    const branch = target.searchParams.get('branch');
    const display = target.pathname === '/display/stream';
    const signedIn = display ? Boolean(getCookie('hp_display')) : Boolean(getCookie('hp_at'));

    setTimeout(() => {
      if (this.readyState === 2) return;
      if (!branch || !signedIn) {
        this.readyState = 2;
        this.fire(new Event('error'));
        return;
      }
      this.readyState = 1;
      this.fire(new Event('open'));
      this.fire(new MessageEvent('board.refresh', { data: JSON.stringify({ branch_id: branch }) }));
      this.off = on(({ topic, data }) => {
        if (topic !== 'queue') return;
        const event = data as { branch_id?: string; type?: string } | undefined;
        if (event?.branch_id && event.branch_id !== branch) return;
        this.fire(new MessageEvent(event?.type ?? 'ticket.updated', { data: JSON.stringify(event ?? {}) }));
      });
    }, LATENCY_MS);
  }

  private fire(event: Event) {
    this.dispatchEvent(event);
    if (event.type === 'open') this.onopen?.(event);
    else if (event.type === 'error') this.onerror?.(event);
    else if (event.type === 'message') this.onmessage?.(event as MessageEvent);
  }

  close() {
    this.readyState = 2;
    this.off?.();
  }
}

window.EventSource = function EventSource(url: string | URL, init?: EventSourceInit) {
  const target = new URL(String(url), location.href);
  if (target.origin === location.origin && STREAM_PATHS.includes(target.pathname)) {
    return new DemoEventSource(url);
  }
  return new RealEventSource(url, init);
} as unknown as typeof EventSource;
Object.assign(window.EventSource, { CONNECTING: 0, OPEN: 1, CLOSED: 2 });

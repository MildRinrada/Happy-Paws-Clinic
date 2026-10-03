import { ApiError, type ApiErrorBody } from './api-error';

/**
 * The single place in the app that calls `fetch` against the backend.
 *
 * Feature modules import this from their own `api/` folder — `features/appointments/api/*` —
 * and never call `fetch` directly. That keeps auth, the error envelope, idempotency keys and
 * optimistic-concurrency headers in one place instead of copy-pasted through components.
 */

export interface RequestOptions {
  /** Query string values; `undefined` entries are dropped. */
  query?: Record<string, string | number | boolean | undefined>;
  /** JSON request body. */
  body?: unknown;
  /** Required by the contract on every POST that creates money, an appointment or a queue ticket. */
  idempotencyKey?: string;
  /** Optimistic concurrency: the `version` last read for this resource. A mismatch answers 409. */
  ifMatch?: string | number;
  signal?: AbortSignal;
  /** Forwarded to Next's fetch cache; defaults to no caching for authenticated reads. */
  cache?: RequestCache;
}

export interface HttpClientConfig {
  baseUrl: string;
  /** Supplies `Authorization` / cookie headers. Server and browser callers pass different ones. */
  authHeaders?: () => Promise<HeadersInit> | HeadersInit;
}

export function createHttpClient({ baseUrl, authHeaders }: HttpClientConfig) {
  async function request<T>(method: string, path: string, options: RequestOptions = {}): Promise<T> {
    const url = new URL(baseUrl.replace(/\/$/, '') + path);

    for (const [key, value] of Object.entries(options.query ?? {})) {
      if (value !== undefined) {
        url.searchParams.set(key, String(value));
      }
    }

    const headers = new Headers(await authHeaders?.());
    headers.set('Accept', 'application/json');

    if (options.body !== undefined) {
      headers.set('Content-Type', 'application/json');
    }
    if (options.idempotencyKey) {
      headers.set('Idempotency-Key', options.idempotencyKey);
    }
    if (options.ifMatch !== undefined) {
      // The version travels as a quoted ETag, which is what the header is defined to hold.
      headers.set('If-Match', `"${options.ifMatch}"`);
    }

    const response = await fetch(url, {
      method,
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal: options.signal,
      cache: options.cache ?? 'no-store',
    });

    if (response.status === 204) {
      return undefined as T;
    }

    const payload = await response.json().catch(() => null);

    if (!response.ok) {
      throw new ApiError(response.status, extractError(payload, response.status));
    }

    return payload as T;
  }

  return {
    get: <T>(path: string, options?: RequestOptions) => request<T>('GET', path, options),
    post: <T>(path: string, options?: RequestOptions) => request<T>('POST', path, options),
    put: <T>(path: string, options?: RequestOptions) => request<T>('PUT', path, options),
    patch: <T>(path: string, options?: RequestOptions) => request<T>('PATCH', path, options),
    delete: <T>(path: string, options?: RequestOptions) => request<T>('DELETE', path, options),
  };
}

export type HttpClient = ReturnType<typeof createHttpClient>;

export function extractError(payload: unknown, status: number): ApiErrorBody {
  const error = (payload as { error?: ApiErrorBody } | null)?.error;

  return (
    error ?? {
      code: 'unexpected_error',
      message: `คำขอล้มเหลว (${status})`,
      message_en: `Request failed with status ${status}`,
      request_id: 'unknown',
    }
  );
}

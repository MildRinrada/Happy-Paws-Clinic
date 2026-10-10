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
  /**
   * Seconds Next may reuse a public read before asking again, in place of `cache`. Only for
   * anonymous reads (`publicApi()`): a cached answer is shared by every visitor.
   */
  revalidate?: number;
}

export interface HttpClientConfig {
  baseUrl: string;
  /** Supplies `Authorization` / cookie headers. Server and browser callers pass different ones. */
  authHeaders?: () => Promise<HeadersInit> | HeadersInit;
}

/** A file the backend answered with instead of JSON, such as a generated PDF. */
export interface FileResponse {
  bytes: ArrayBuffer;
  /** The name from `Content-Disposition`, when the backend gave one. */
  fileName?: string;
}

/** `filename="..."` out of a `Content-Disposition`. */
function fileNameFrom(disposition: string | null): string | undefined {
  const match = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
  return match?.[1] ? decodeURIComponent(match[1]) : undefined;
}

export function createHttpClient({ baseUrl, authHeaders }: HttpClientConfig) {
  async function send(method: string, path: string, options: RequestOptions, accept: string): Promise<Response> {
    const url = new URL(baseUrl.replace(/\/$/, '') + path);

    for (const [key, value] of Object.entries(options.query ?? {})) {
      if (value !== undefined) {
        url.searchParams.set(key, String(value));
      }
    }

    const headers = new Headers(await authHeaders?.());
    headers.set('Accept', accept);

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
      ...(options.revalidate === undefined
        ? { cache: options.cache ?? 'no-store' }
        : { next: { revalidate: options.revalidate } }),
    });

    // A refusal is the usual JSON error envelope whatever was asked for.
    if (!response.ok) {
      throw new ApiError(response.status, extractError(await response.json().catch(() => null), response.status));
    }

    return response;
  }

  async function request<T>(method: string, path: string, options: RequestOptions = {}): Promise<T> {
    const response = await send(method, path, options, 'application/json');

    if (response.status === 204) {
      return undefined as T;
    }

    return (await response.json().catch(() => null)) as T;
  }

  /** A POST answered with a file rather than JSON. */
  async function postForFile(path: string, accept: string, options: RequestOptions = {}): Promise<FileResponse> {
    const response = await send('POST', path, options, `${accept}, application/json`);

    return {
      bytes: await response.arrayBuffer(),
      fileName: fileNameFrom(response.headers.get('Content-Disposition')),
    };
  }

  return {
    postForFile,
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

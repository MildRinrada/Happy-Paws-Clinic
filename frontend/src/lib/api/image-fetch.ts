/**
 * Plain `fetch` for pictures the server draws itself, such as the clinic's logo fitted into the tab
 * icon (docs/contract-extensions.md § 50). Not the shared client: these answers are a redirect and
 * raw image bytes, not JSON, and a picture that cannot be read is an ordinary outcome, not an error.
 */

export interface ImageBytes {
  bytes: Uint8Array;
  /** The media type the answer declared, lowercased, without parameters. */
  contentType: string;
}

/** Where a public read redirects to, without following it, or undefined when it does not redirect. */
export async function redirectLocation(url: string): Promise<string | undefined> {
  const response = await fetch(url, { redirect: 'manual', cache: 'no-store', signal: AbortSignal.timeout(5000) });
  const location = response.headers.get('location');

  return response.status >= 300 && response.status < 400 && location ? location : undefined;
}

/** A picture's bytes, or undefined when it cannot be read or is larger than `maxBytes`. */
export async function fetchImageBytes(url: string, maxBytes: number): Promise<ImageBytes | undefined> {
  const response = await fetch(url, { cache: 'no-store', signal: AbortSignal.timeout(5000) });
  if (!response.ok || Number(response.headers.get('content-length') ?? 0) > maxBytes) return undefined;

  const bytes = new Uint8Array(await response.arrayBuffer());
  if (bytes.length > maxBytes) return undefined;

  return { bytes, contentType: (response.headers.get('content-type') ?? '').split(';')[0]!.trim().toLowerCase() };
}

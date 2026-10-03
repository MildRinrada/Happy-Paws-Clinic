import type { NextRequest } from 'next/server';
import { openQueueStream } from '@/features/queue/api/openQueueStream';
import { readAccessToken } from '@/lib/auth/session';

// A stream is never prerendered or cached, and it needs a runtime that can hold a socket open.
export const dynamic = 'force-dynamic';
export const runtime = 'nodejs';

/**
 * The browser's end of the queue stream.
 *
 * `EventSource` cannot send an `Authorization` header and the access token is in a cookie script
 * cannot read, so the browser connects here, on its own origin, and this attaches the token and
 * pipes the backend's stream straight through. The token never appears in a URL, where it would
 * be written into every access log along the way.
 *
 * The middleware has already refreshed the session by the time this runs, so a reconnect after
 * the access token lapsed arrives here with a new one.
 */
export async function GET(request: NextRequest) {
  const branchId = request.nextUrl.searchParams.get('branch');
  const accessToken = await readAccessToken();

  if (!branchId) {
    return new Response(null, { status: 400 });
  }
  if (!accessToken) {
    return new Response(null, { status: 401 });
  }

  const upstream = await openQueueStream(branchId, accessToken, request.signal);

  if (!upstream.ok || !upstream.body) {
    // 401 and 403 are passed on as they are, so the client stops rather than retrying forever
    // against a stream this session may not watch.
    return new Response(null, { status: upstream.status });
  }

  return new Response(upstream.body, {
    headers: {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache, no-transform',
      Connection: 'keep-alive',
      // Tells a fronting nginx not to buffer: a buffered queue board updates in bursts, late.
      'X-Accel-Buffering': 'no',
    },
  });
}

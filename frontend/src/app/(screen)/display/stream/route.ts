import type { NextRequest } from 'next/server';
import { openDisplayStream } from '@/features/queue/api/displayBoard';
import { readDisplayToken } from '@/lib/display/display-session';

// A stream is never prerendered or cached, and it needs a runtime that can hold a socket open.
export const dynamic = 'force-dynamic';
export const runtime = 'nodejs';

/**
 * The token screen's end of the queue stream (docs/contract-extensions.md § 30).
 *
 * The same job as `/api/queue/stream` for the console, with the screen's own credential: the
 * display token from the `hp_display` cookie, which is scoped to `/display` and so reaches this
 * handler and not the console's. No staff cookie is read, and the middleware does not run here.
 * The backend sends this audience the branch id only, never a ticket id.
 */
export async function GET(request: NextRequest) {
  const branchId = request.nextUrl.searchParams.get('branch');
  const token = await readDisplayToken();

  if (!branchId) {
    return new Response(null, { status: 400 });
  }
  if (!token) {
    return new Response(null, { status: 401 });
  }

  const upstream = await openDisplayStream(branchId, token, request.signal);

  if (!upstream.ok || !upstream.body) {
    // 401 (revoked) and 403 (another branch) pass through, so the browser stops rather than
    // spinning; the page's own once-a-minute re-read then shows why.
    return new Response(null, { status: upstream.status });
  }

  return new Response(upstream.body, {
    headers: {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache, no-transform',
      Connection: 'keep-alive',
      'X-Accel-Buffering': 'no',
    },
  });
}

import { openEventStream } from '@/lib/api/event-stream';

/** `SSE /queue/stream?branch=` — "event: ticket.called · ticket.updated · board.refresh". */
export function openQueueStream(branchId: string, accessToken: string, signal: AbortSignal): Promise<Response> {
  return openEventStream('/queue/stream', { branch: branchId }, accessToken, signal);
}

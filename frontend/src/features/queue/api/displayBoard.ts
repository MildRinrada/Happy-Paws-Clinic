import { openEventStream } from '@/lib/api/event-stream';
import { displayApi, publicApi } from '@/lib/api/server-client';
import type { ClaimedQueueDisplay, DisplayBoard } from '../types/queue.types';

/**
 * The waiting-room screen's own calls, made with its display token rather than a staff session
 * (docs/contract-extensions.md § 30).
 */

/** `POST /queue/displays/claim` — trades the setup code for the token. Anonymous; capped per address. */
export function claimQueueDisplay(code: string): Promise<ClaimedQueueDisplay> {
  return publicApi().post<ClaimedQueueDisplay>('/queue/displays/claim', { body: { code } });
}

/** `GET /queue/display-board` — today's numbers, rooms and vet names for the token's branch. */
export function getDisplayBoard(displayToken: string): Promise<DisplayBoard> {
  return displayApi(displayToken).get<DisplayBoard>('/queue/display-board');
}

/** `SSE /queue/stream?branch=` as the screen. Events carry the branch only, never a ticket id. */
export function openDisplayStream(branchId: string, displayToken: string, signal: AbortSignal): Promise<Response> {
  return openEventStream('/queue/stream', { branch: branchId }, displayToken, signal);
}

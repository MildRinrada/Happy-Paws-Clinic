'use client';

import { useEffect, useRef, useState } from 'react';

/** The three events the contract's stream sends: "ticket.called · ticket.updated · board.refresh". */
const EVENTS = ['ticket.called', 'ticket.updated', 'board.refresh'] as const;

/** Back-off for a stream the browser gave up on: 2 s, doubling, never more than a minute apart. */
const RETRY_FIRST_MS = 2_000;
const RETRY_MAX_MS = 60_000;

export type StreamState = 'connecting' | 'live' | 'reconnecting';

/**
 * Subscribes to one branch's queue stream and calls back when the board has changed.
 *
 * The events are treated as "something changed — read the board again", never as the change
 * itself. The stream says which ticket moved, not the state of the whole board, and a client that
 * patched its own copy from events would drift the first time one was missed. Re-reading keeps the
 * screen equal to the server by construction.
 *
 * Bursts are folded: closing a case fires several events in a row, and one re-read answers all of
 * them.
 *
 * Reconnecting has two halves. A line that drops mid-stream is retried by `EventSource` itself.
 * A connection that is refused (401, 403, a 502 while the API restarts) makes the browser close
 * the source for good, so the hook opens a new one after a growing pause. A screen left on all
 * day, the waiting-room display above all, therefore comes back by itself once the API does; the
 * new request passes the middleware, which refreshes the session first.
 */
export function useQueueStream(branchId: string, onChange: () => void): StreamState {
  const [state, setState] = useState<StreamState>('connecting');

  // Held in a ref so a new callback identity on each render does not tear the connection down.
  const notify = useRef(onChange);

  useEffect(() => {
    notify.current = onChange;
  }, [onChange]);

  useEffect(() => {
    let source: EventSource | undefined;
    let pending: ReturnType<typeof setTimeout> | undefined;
    let retry: ReturnType<typeof setTimeout> | undefined;
    let delay = RETRY_FIRST_MS;

    const changed = () => {
      clearTimeout(pending);
      pending = setTimeout(() => notify.current(), 150);
    };

    const connect = () => {
      // Same origin: the route handler attaches the bearer token the browser cannot read.
      const current = new EventSource(`/api/queue/stream?branch=${encodeURIComponent(branchId)}`);
      source = current;

      current.onopen = () => {
        delay = RETRY_FIRST_MS;
        setState('live');
      };

      // The backend opens every connection with `board.refresh`, so whatever happened while the
      // line was down is picked up the moment it comes back.
      current.onerror = () => {
        setState('reconnecting');

        if (current.readyState === EventSource.CLOSED) {
          retry = setTimeout(connect, delay);
          delay = Math.min(delay * 2, RETRY_MAX_MS);
        }
      };

      for (const name of EVENTS) {
        current.addEventListener(name, changed);
      }
    };

    connect();

    return () => {
      clearTimeout(pending);
      clearTimeout(retry);
      source?.close();
    };
  }, [branchId]);

  return state;
}

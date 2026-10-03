'use client';

import { useEffect, useRef, useState } from 'react';

/** The three events the contract's stream sends: "ticket.called · ticket.updated · board.refresh". */
const EVENTS = ['ticket.called', 'ticket.updated', 'board.refresh'] as const;

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
 */
export function useQueueStream(branchId: string, onChange: () => void): StreamState {
  const [state, setState] = useState<StreamState>('connecting');

  // Held in a ref so a new callback identity on each render does not tear the connection down.
  const notify = useRef(onChange);

  useEffect(() => {
    notify.current = onChange;
  }, [onChange]);

  useEffect(() => {
    // Same origin: the route handler attaches the bearer token the browser cannot read.
    const source = new EventSource(`/api/queue/stream?branch=${encodeURIComponent(branchId)}`);
    let pending: ReturnType<typeof setTimeout> | undefined;

    const changed = () => {
      clearTimeout(pending);
      pending = setTimeout(() => notify.current(), 150);
    };

    source.onopen = () => setState('live');

    // EventSource retries on its own. The backend opens every connection with `board.refresh`, so
    // whatever happened while the line was down is picked up the moment it comes back.
    source.onerror = () => setState('reconnecting');

    for (const name of EVENTS) {
      source.addEventListener(name, changed);
    }

    return () => {
      clearTimeout(pending);
      source.close();
    };
  }, [branchId]);

  return state;
}

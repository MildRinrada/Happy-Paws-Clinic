import { reload } from './db';

/**
 * What the demo's backend says when something changes: the queue moved, a message went out. The
 * stream shims and the demo bar listen here. Other tabs of the demo hear it too, so a waiting-room
 * screen open in a second tab follows the console in the first, as it would on a real network.
 */

export interface BusEvent {
  topic: string;
  data?: unknown;
}

type Listener = (event: BusEvent) => void;

const listeners = new Set<Listener>();
const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel('hp-demo');

channel?.addEventListener('message', (message: MessageEvent<BusEvent>) => {
  // The other tab has written its change to storage; read it before anyone looks.
  reload();
  for (const listener of listeners) listener(message.data);
});

export function emit(topic: string, data?: unknown): void {
  const event = { topic, data };
  // Let the change reach storage first, so another tab reads the new state.
  queueMicrotask(() => {
    for (const listener of listeners) listener(event);
    channel?.postMessage(event);
  });
}

export function on(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

import { kindLabels } from '../content/message-labels';
import type { ThreadMessage } from '../types/thread.types';

/** What a message says on screen: its words, or what kind of thing was sent, with its name or place. */
export function messageText(message: Pick<ThreadMessage, 'kind' | 'text'>): string {
  if (message.kind === 'text') return message.text ?? '';

  const label = kindLabels[message.kind] ?? kindLabels.other ?? '';
  return message.text ? `${label}: ${message.text}` : label;
}

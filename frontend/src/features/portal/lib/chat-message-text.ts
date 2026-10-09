import { portalChat } from '../content/portal-chat';
import type { ChatMessage } from '../types/chat.types';

/** What a chat message says on screen: its words, or what kind of thing was sent, with its name or place. */
export function chatMessageText(message: Pick<ChatMessage, 'kind' | 'text'>): string {
  if (message.kind === 'text') return message.text ?? '';

  const label = portalChat.kinds[message.kind] ?? portalChat.kinds.other ?? '';
  return message.text ? `${label}: ${message.text}` : label;
}

/** Who wrote it and, for the owner's own message from another channel, where from. */
export function chatMessageMeta(message: ChatMessage): string {
  if (message.direction === 'outbound') return message.sent_by_name || portalChat.clinic;

  const channel = message.channel && message.channel !== 'portal' ? portalChat.channels[message.channel] : undefined;
  return channel ? `${portalChat.you} ${portalChat.sentVia(channel)}` : portalChat.you;
}

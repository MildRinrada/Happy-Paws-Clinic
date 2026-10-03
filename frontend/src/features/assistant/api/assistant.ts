import { publicApi } from '@/lib/api/server-client';
import type { ChatMessage, ChatReply } from '../types/assistant.types';

/** `GET /assistant` — whether the clinic has an assistant switched on. */
export async function getAssistantStatus(): Promise<{ enabled: boolean }> {
  return publicApi().get<{ enabled: boolean }>('/assistant');
}

/** `POST /assistant/chat` — the next reply to the conversation so far. Nothing is stored server-side. */
export function chat(messages: ChatMessage[]): Promise<ChatReply> {
  return publicApi().post<ChatReply>('/assistant/chat', { body: { messages } });
}

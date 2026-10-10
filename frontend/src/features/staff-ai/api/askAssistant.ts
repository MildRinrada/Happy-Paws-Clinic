import { staffApi } from '@/lib/api/server-client';
import type { AskMessage, AskReply } from '../types/staff-ai.types';

/** `POST /staff/ai/ask`: the conversation so far in, the next answer out. Nothing is stored. */
export function askAssistant(messages: AskMessage[]): Promise<AskReply> {
  return staffApi().post<AskReply>('/staff/ai/ask', { body: { messages } });
}

import type { ThreadStatus } from '../types/thread.types';

/** The inbox's address. */
export const MESSAGES_PATH = '/messages';

/** What the address says: the open thread, the filter, and the page of the list. */
export interface MessagesQuery {
  thread?: string;
  status?: ThreadStatus;
  cursor?: string;
}

function first(raw: string | string[] | undefined): string | undefined {
  return (Array.isArray(raw) ? raw[0] : raw)?.trim().slice(0, 200) || undefined;
}

/** Keeps the known values; anything else in the address is ignored. */
export function readMessagesQuery(params: Record<string, string | string[] | undefined>): MessagesQuery {
  const status = first(params.status);

  return {
    thread: first(params.thread)?.slice(0, 40),
    status: status === 'awaiting_reply' || status === 'replied' ? status : undefined,
    cursor: first(params.cursor),
  };
}

/** The inbox with some values changed. An absent value is left out of the address. */
export function messagesHref(query: MessagesQuery, change: Partial<MessagesQuery> = {}): string {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries({ ...query, ...change })) {
    if (value) params.set(key, value);
  }

  const search = params.toString();
  return search ? `${MESSAGES_PATH}?${search}` : MESSAGES_PATH;
}

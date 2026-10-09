/**
 * One message of the owner's conversation with the clinic, as `GET /portal/thread` gives it
 * (contract extension § 24). No staff id and no delivery details: those are the clinic's.
 */
export interface ChatMessage {
  id: string;
  /** `inbound` is the owner's own message, `outbound` the clinic's answer. */
  direction: 'inbound' | 'outbound';
  /** Where the owner wrote from (`portal`, `line`...). Only on their own messages. */
  channel?: string;
  kind: 'text' | 'image' | 'video' | 'audio' | 'file' | 'location' | 'sticker' | 'other';
  /** Absent for a picture or a sticker: only words are kept. */
  text?: string;
  sent_at: string;
  /** Who answered, on the clinic's messages. The page says "คลินิก" when absent. */
  sent_by_name?: string;
}

/** The owner's thread and one page of it, newest first. No `id` until they first write. */
export interface ChatThread {
  id?: string;
  status?: 'awaiting_reply' | 'replied';
  data: ChatMessage[];
  page: { next_cursor?: string | null };
}

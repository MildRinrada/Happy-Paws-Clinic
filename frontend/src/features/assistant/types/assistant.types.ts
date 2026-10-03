/** One line of the conversation, as the widget keeps it and the API takes it. */
export interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
}

/** A button under a reply. Always a path on this site; the API builds them, not the model. */
export interface AssistantAction {
  label: string;
  href: string;
}

/** `POST /assistant/chat` — a contract extension (§ 16). */
export interface ChatReply {
  reply: string;
  actions: AssistantAction[];
}

/** What the widget draws: the message, and on an assistant reply its buttons. */
export interface ChatEntry extends ChatMessage {
  actions?: AssistantAction[];
}

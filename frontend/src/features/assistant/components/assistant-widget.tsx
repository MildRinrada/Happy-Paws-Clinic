import { getAssistantStatus } from '../api/assistant';
import { ChatLauncher } from './chat-launcher';

/**
 * The chat button, drawn only when the clinic has an assistant switched on. A clinic with no model
 * configured, or an API that cannot be reached, simply has no button: never one that opens onto
 * an error.
 */
export async function AssistantWidget() {
  const enabled = await getAssistantStatus().then((status) => status.enabled).catch(() => false);

  return enabled ? <ChatLauncher /> : null;
}

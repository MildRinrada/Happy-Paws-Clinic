import { Badge, type BadgeTone } from '@/components/ui/badge';
import { apiKeyListText } from '../content/api-key-list';
import type { ApiKeyStatus } from '../types/integration.types';

const tones: Record<ApiKeyStatus, BadgeTone> = {
  active: 'green',
  expired: 'neutral',
  revoked: 'red',
};

/** Usable, expired or revoked. */
export function ApiKeyStatusBadge({ status }: { status: ApiKeyStatus }) {
  return <Badge tone={tones[status]}>{apiKeyListText.status[status]}</Badge>;
}

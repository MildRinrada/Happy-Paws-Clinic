import { Alert } from '@/components/ui/alert';
import { providerShared } from '../content/provider-settings';

/** Says plainly that these settings are stored and nothing yet uses them. */
export function ProviderNotice() {
  return <Alert tone="info">{providerShared.notConnected}</Alert>;
}

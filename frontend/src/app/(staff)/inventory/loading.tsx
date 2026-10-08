import { PageLoading } from '@/components/ui/page-loading';
import { uiText } from '@/config/ui-text';

/** Below the tabs, so switching tab keeps the tabs where they are. */
export default function InventoryLoading() {
  return <PageLoading label={uiText.loadingPage} />;
}

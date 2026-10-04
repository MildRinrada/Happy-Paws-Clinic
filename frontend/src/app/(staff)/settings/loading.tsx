import { PageLoading } from '@/components/ui/page-loading';
import { uiText } from '@/config/ui-text';

/** Below the settings tabs, so switching tab keeps the tabs where they are. */
export default function SettingsLoading() {
  return <PageLoading label={uiText.loadingPage} />;
}

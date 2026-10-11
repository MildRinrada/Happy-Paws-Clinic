import { PageLoading } from '@/components/ui/page-loading';
import { uiText } from '@/config/ui-text';

/** Beside the settings pages list, so switching page keeps the list where it is. */
export default function SettingsLoading() {
  return <PageLoading label={uiText.loadingPage} />;
}

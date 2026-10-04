import { PageLoading } from '@/components/ui/page-loading';
import { uiText } from '@/config/ui-text';

/** Shown inside the portal's frame while a page's data loads. */
export default function PortalLoading() {
  return <PageLoading label={uiText.loadingPage} />;
}

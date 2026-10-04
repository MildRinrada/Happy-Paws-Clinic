import { PageLoading } from '@/components/ui/page-loading';
import { uiText } from '@/config/ui-text';

/** Shown inside the console's frame while a page's data loads, so the menu stays put. */
export default function StaffLoading() {
  return <PageLoading label={uiText.loadingPage} />;
}

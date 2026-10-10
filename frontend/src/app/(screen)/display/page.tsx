import type { Metadata } from 'next';
import { redirect } from 'next/navigation';
import { DisplayMessage } from '@/features/queue/components/display/display-message';
import { DisplayScreen } from '@/features/queue/components/display/display-screen';
import { ScreenStopped } from '@/features/queue/components/display/screen-stopped';
import { queueDisplay } from '@/features/queue/content/queue-display';
import { loadScreenView, type ScreenView } from '@/features/queue/lib/load-screen-view';
import { ApiError } from '@/lib/api/api-error';
import { DISPLAY_SETUP_PATH, DISPLAY_STREAM_PATH } from '@/lib/display/display-paths';
import { readDisplayToken } from '@/lib/display/display-session';

export const metadata: Metadata = { title: queueDisplay.metaTitle };

/**
 * The waiting-room screen on its own token (docs/contract-extensions.md § 30): the same layout as
 * `/queue/display`, read from `GET /queue/display-board` with the token in this browser's
 * `hp_display` cookie. No staff session is read or needed.
 *
 * - No cookie: this browser was never set up, so it goes to `/display/setup`.
 * - `401`: the screen was revoked or has expired. It says so and stays put; nothing retries a
 *   credential that has been withdrawn.
 * - Anything else that fails is the error boundary's, which retries every 15 seconds.
 */
export default async function ScreenPage() {
  const token = await readDisplayToken();

  if (!token) {
    redirect(DISPLAY_SETUP_PATH);
  }

  let screen: ScreenView;

  try {
    screen = await loadScreenView(token);
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return <ScreenStopped />;
    }
    if (error instanceof ApiError && error.status === 403) {
      return <DisplayMessage title={queueDisplay.screen.wrongBranch} />;
    }
    throw error;
  }

  return (
    <DisplayScreen view={screen.view} branchId={screen.branchId} branchName={screen.branchName} streamPath={DISPLAY_STREAM_PATH} />
  );
}

import Link from 'next/link';
import { DISPLAY_SETUP_PATH } from '@/lib/display/display-paths';
import { queueDisplay } from '../../content/queue-display';
import { DisplayMessage } from './display-message';

/** A screen whose token was revoked or has run out: says so, and where to set it up again. */
export function ScreenStopped() {
  const text = queueDisplay.screen;

  return (
    <DisplayMessage title={text.stopped}>
      <p>
        {text.stoppedBody}{' '}
        <Link href={DISPLAY_SETUP_PATH} className="font-semibold text-white underline underline-offset-4">
          {text.setupLink}
        </Link>
      </p>
    </DisplayMessage>
  );
}

'use client';

import { useEffect } from 'react';
import { markRepliesReadAction } from '../../actions/chat-actions';

/**
 * Clears the bell for the clinic's answers the owner now has on screen. Runs again only when a
 * newer answer arrives, so the page's own refresh does not repeat it. A failure changes nothing
 * the owner can see beyond the bell, so it is not reported.
 */
export function MarkRepliesRead({ latestReplyId }: { latestReplyId?: string }) {
  useEffect(() => {
    if (!latestReplyId) return;

    markRepliesReadAction().catch(() => undefined);
  }, [latestReplyId]);

  return null;
}

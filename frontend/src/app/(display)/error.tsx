'use client';

import { useRouter } from 'next/navigation';
import { startTransition, useEffect } from 'react';
import { DisplayMessage } from '@/features/queue/components/display/display-message';
import { queueDisplay } from '@/features/queue/content/queue-display';

/** How long the screen waits before trying the board again. */
const RETRY_MS = 15_000;

/**
 * A fault on the waiting-room screen (API down, a 500) shows a calm line and retries by itself.
 * Nobody is standing at a TV to press "ลองใหม่", so the console's error boundary would leave it
 * stuck on an error until someone noticed.
 */
export default function DisplayError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  const router = useRouter();

  useEffect(() => {
    const timer = setTimeout(() => {
      startTransition(() => {
        router.refresh();
        reset();
      });
    }, RETRY_MS);

    return () => clearTimeout(timer);
  }, [router, reset]);

  return <DisplayMessage title={queueDisplay.error.title}>{queueDisplay.error.body}</DisplayMessage>;
}

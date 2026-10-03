'use client';

import { useRouter } from 'next/navigation';
import { useCallback } from 'react';
import { useQueueStream, type StreamState } from '../hooks/use-queue-stream';

const labels: Record<StreamState, { text: string; dot: string }> = {
  connecting: { text: 'กำลังเชื่อมต่อ', dot: 'bg-ink-300' },
  live: { text: 'อัปเดตสด', dot: 'bg-emerald-500' },
  reconnecting: { text: 'หลุดการเชื่อมต่อ · กำลังต่อใหม่', dot: 'bg-amber-500' },
};

/**
 * Keeps the board current and says whether it is.
 *
 * On every stream event the page's server components are re-run, which re-reads the board from
 * the backend. The indicator matters as much as the refresh: a board that has silently stopped
 * updating looks exactly like a quiet morning, and a receptionist needs to be able to tell them
 * apart.
 */
export function QueueLiveIndicator({ branchId }: { branchId: string }) {
  const router = useRouter();
  const refresh = useCallback(() => router.refresh(), [router]);
  const label = labels[useQueueStream(branchId, refresh)];

  return (
    <p role="status" className="flex items-center gap-2 text-sm text-ink-600">
      <span aria-hidden className={`size-2 rounded-full ${label.dot}`} />
      {label.text}
    </p>
  );
}

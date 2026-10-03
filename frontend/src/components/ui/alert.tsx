import type { ReactNode } from 'react';

type Tone = 'error' | 'info' | 'success';

const tones: Record<Tone, string> = {
  error: 'border-red-200 bg-red-50 text-red-800',
  info: 'border-sky-200 bg-sky-50 text-sky-900',
  success: 'border-emerald-200 bg-emerald-50 text-emerald-900',
};

/** An inline message. Errors are announced immediately; the rest wait their turn. */
export function Alert({ tone = 'error', children }: { tone?: Tone; children: ReactNode }) {
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className={`rounded-lg border px-3.5 py-2.5 text-sm ${tones[tone]}`}
    >
      {children}
    </div>
  );
}

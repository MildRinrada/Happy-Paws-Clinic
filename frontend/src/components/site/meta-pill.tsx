import type { ReactNode } from 'react';

export type MetaPillTone = 'white' | 'soft' | 'softForest' | 'sun';

const tones: Record<MetaPillTone, string> = {
  // On a grey card.
  white: 'bg-white text-ink-700',
  // On a white card.
  soft: 'bg-site-soft text-ink-700',
  // The same grey with dark green words: a person's field under their name on the team page.
  softForest: 'bg-site-soft text-site-forest',
  // Yellow with bold dark words, to stand out on the dark green: "บทความล่าสุด".
  sun: 'bg-site-sun font-semibold text-ink-900',
};

/** A small rounded label beside or above a title: how long a service takes, and the like. */
export function MetaPill({
  tone = 'white',
  className = '',
  children,
}: {
  tone?: MetaPillTone;
  className?: string;
  children: ReactNode;
}) {
  return (
    <span className={`inline-flex h-[22px] items-center whitespace-nowrap rounded-full px-2.5 text-xs ${tones[tone]} ${className}`}>
      {children}
    </span>
  );
}

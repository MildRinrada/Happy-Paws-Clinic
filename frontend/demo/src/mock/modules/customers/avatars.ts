/**
 * Small drawn portraits for the sample pets, as SVG data URLs: the demo has no photo files to
 * ship, and a picture makes the portal's pet cards read as someone's animals.
 */

const KINDS: Record<string, { bg: string; fur: string; inner: string; ears: 'pointed' | 'floppy' | 'long' }> = {
  shiba: { bg: '#fde7d3', fur: '#d9822b', inner: '#fff4e6', ears: 'pointed' },
  orange: { bg: '#fff1c9', fur: '#f0a336', inner: '#fde3b0', ears: 'pointed' },
  rabbit: { bg: '#e7eefc', fur: '#c9a98a', inner: '#f3e3d3', ears: 'long' },
  dog: { bg: '#e6f4ea', fur: '#8b6b4a', inner: '#e9d8c4', ears: 'floppy' },
};

export function petAvatar(kind: string): string {
  const k = KINDS[kind] ?? KINDS.dog!;
  const ears =
    k.ears === 'pointed'
      ? `<path d="M30 42 L38 14 L52 34Z M90 42 L82 14 L68 34Z" fill="${k.fur}"/>`
      : k.ears === 'long'
        ? `<ellipse cx="46" cy="22" rx="9" ry="24" fill="${k.fur}"/><ellipse cx="74" cy="22" rx="9" ry="24" fill="${k.fur}"/><ellipse cx="46" cy="24" rx="4" ry="17" fill="${k.inner}"/><ellipse cx="74" cy="24" rx="4" ry="17" fill="${k.inner}"/>`
        : `<ellipse cx="30" cy="56" rx="11" ry="20" fill="${k.fur}"/><ellipse cx="90" cy="56" rx="11" ry="20" fill="${k.fur}"/>`;
  const svg =
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 120"><rect width="120" height="120" rx="24" fill="${k.bg}"/>` +
    ears +
    `<circle cx="60" cy="64" r="32" fill="${k.fur}"/><ellipse cx="60" cy="78" rx="20" ry="15" fill="${k.inner}"/>` +
    `<circle cx="48" cy="60" r="4" fill="#2b2118"/><circle cx="72" cy="60" r="4" fill="#2b2118"/>` +
    `<ellipse cx="60" cy="72" rx="5" ry="3.5" fill="#2b2118"/><path d="M54 80 Q60 85 66 80" stroke="#2b2118" stroke-width="2" fill="none" stroke-linecap="round"/></svg>`;
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}

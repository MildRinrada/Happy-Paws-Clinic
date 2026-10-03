/**
 * A short name for a device from its user-agent string, such as "Chrome บน Android". A rough read,
 * only to help an owner recognise their own phone in a list; never used to decide anything.
 */

const systems: [RegExp, string][] = [
  [/iPhone|iPad/, 'iPhone/iPad'],
  [/Android/, 'Android'],
  [/Windows/, 'Windows'],
  [/Mac OS X|Macintosh/, 'Mac'],
  [/Linux/, 'Linux'],
];

// Order matters: Edge and the LINE in-app browser also say "Chrome", and Chrome also says "Safari".
const browsers: [RegExp, string][] = [
  [/Line\//, 'LINE'],
  [/Edg\//, 'Edge'],
  [/Firefox\//, 'Firefox'],
  [/Chrome\//, 'Chrome'],
  [/Safari\//, 'Safari'],
];

export function deviceLabel(userAgent: string | null | undefined): string | undefined {
  if (!userAgent) return undefined;

  const system = systems.find(([pattern]) => pattern.test(userAgent))?.[1];
  const browser = browsers.find(([pattern]) => pattern.test(userAgent))?.[1];

  if (browser && system) return `${browser} บน ${system}`;
  return browser ?? system;
}

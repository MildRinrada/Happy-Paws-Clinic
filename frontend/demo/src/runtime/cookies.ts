/**
 * The cookies `next/headers` reads and writes, kept in this browser's storage. In the product they
 * are httpOnly and live on the server; in the demo the "server" is this page, so a plain store is
 * the same thing: the session survives a reload and stays in this browser.
 */

const KEY = 'hp-demo-cookies';

interface Stored {
  value: string;
  expires: number | null;
}

let jar: Record<string, Stored> = load();

function load(): Record<string, Stored> {
  try {
    return JSON.parse(localStorage.getItem(KEY) ?? '{}') as Record<string, Stored>;
  } catch {
    return {};
  }
}

function persist() {
  try {
    localStorage.setItem(KEY, JSON.stringify(jar));
  } catch {
    // Private windows may refuse storage; the session then lasts as long as the tab.
  }
}

function live(name: string): Stored | undefined {
  const entry = jar[name];
  if (!entry) return undefined;
  if (entry.expires !== null && entry.expires <= Date.now()) {
    delete jar[name];
    persist();
    return undefined;
  }
  return entry;
}

export interface CookieOptions {
  maxAge?: number;
  expires?: Date | number;
  [key: string]: unknown;
}

/** Cookies script writes itself (the sidebar's open or closed) are in `document.cookie`, as in the product. */
function fromDocument(name: string): string | undefined {
  const pair = document.cookie.split('; ').find((entry) => entry.startsWith(`${name}=`));
  return pair === undefined ? undefined : decodeURIComponent(pair.slice(name.length + 1));
}

/** A cookie the product does not mark httpOnly is one its own script reads, so it goes in `document.cookie` too. */
function mirror(name: string, value: string, maxAgeSeconds: number | null) {
  const age = maxAgeSeconds === null ? '' : `; max-age=${Math.max(0, Math.round(maxAgeSeconds))}`;
  document.cookie = `${name}=${encodeURIComponent(value)}; path=/${age}; samesite=lax`;
}

export function getCookie(name: string): string | undefined {
  return live(name)?.value ?? fromDocument(name);
}

export function setCookie(name: string, value: string, options: CookieOptions = {}): void {
  let expires: number | null = null;
  if (typeof options.maxAge === 'number') expires = Date.now() + options.maxAge * 1000;
  else if (options.expires instanceof Date) expires = options.expires.getTime();
  else if (typeof options.expires === 'number') expires = options.expires;

  if (expires !== null && expires <= Date.now()) {
    delete jar[name];
  } else {
    jar[name] = { value, expires };
  }
  persist();
  if (options.httpOnly === false) mirror(name, value, expires === null ? null : (expires - Date.now()) / 1000);
}

export function deleteCookie(name: string): void {
  delete jar[name];
  persist();
  if (fromDocument(name) !== undefined) mirror(name, '', 0);
}

export function allCookies(): { name: string; value: string }[] {
  return Object.keys(jar)
    .map((name) => ({ name, value: live(name)?.value }))
    .filter((cookie): cookie is { name: string; value: string } => cookie.value !== undefined);
}

export function clearCookies(): void {
  for (const name of Object.keys(jar)) {
    if (fromDocument(name) !== undefined) mirror(name, '', 0);
  }
  jar = {};
  persist();
}

import { allCookies, deleteCookie, getCookie, setCookie, type CookieOptions } from '@demo/runtime/cookies';

/** `next/headers`: the cookie jar is this browser's demo store; the request headers are the browser's own. */

const jar = {
  get(name: string) {
    const value = getCookie(name);
    return value === undefined ? undefined : { name, value };
  },
  getAll() {
    return allCookies();
  },
  has(name: string) {
    return getCookie(name) !== undefined;
  },
  set(nameOrCookie: string | ({ name: string; value: string } & CookieOptions), value?: string, options?: CookieOptions) {
    if (typeof nameOrCookie === 'string') setCookie(nameOrCookie, value ?? '', options);
    else setCookie(nameOrCookie.name, nameOrCookie.value, nameOrCookie);
    return jar;
  },
  delete(name: string) {
    deleteCookie(name);
    return jar;
  },
  toString() {
    return allCookies()
      .map((cookie) => `${cookie.name}=${encodeURIComponent(cookie.value)}`)
      .join('; ');
  },
};

export async function cookies() {
  return jar;
}

export async function headers() {
  return new Headers({ 'user-agent': navigator.userAgent, 'accept-language': 'th-TH' });
}

export async function draftMode() {
  return { isEnabled: false, enable() {}, disable() {} };
}

import { NextResponse, type NextRequest } from 'next/server';
import { NEXT_PARAM } from '@/features/authentication/lib/home-path';
import { refreshSession } from '@/lib/auth/refresh-session';
import {
  ACCESS_COOKIE,
  REFRESH_COOKIE,
  clearedCookieWrites,
  sessionCookieWrites,
} from '@/lib/auth/session-cookies';

/**
 * Keeps the session alive, once per request, before anything renders.
 *
 * Server components cannot set cookies, so a page cannot refresh its own token. This runs ahead of
 * them: when the access cookie has lapsed and a refresh cookie remains, it rotates the pair,
 * writes the new cookies onto the response for the browser, and rewrites the request's own cookie
 * header so the page, the action or the stream behind it sees a signed-in request.
 *
 * It decides nothing about what a user may do. Every backend call is authorized by the backend;
 * all this does is send someone with no session to the sign-in page instead of a screen of 401s.
 */

const SIGN_IN_PATH = '/login';

function isApiRoute(pathname: string) {
  return pathname.startsWith('/api/');
}

function withCookies(response: NextResponse, writes: ReturnType<typeof sessionCookieWrites>) {
  for (const { name, value, options } of writes) {
    response.cookies.set(name, value, options);
  }
  return response;
}

/** Where a request with no session goes: a 401 for a fetch, the sign-in page for a navigation. */
function unauthenticated(request: NextRequest) {
  if (isApiRoute(request.nextUrl.pathname)) {
    return withCookies(new NextResponse(null, { status: 401 }), clearedCookieWrites());
  }

  // Remember where they were going, so signing in leads back there (see `destinationFor`).
  const signIn = new URL(SIGN_IN_PATH, request.url);
  signIn.searchParams.set(NEXT_PARAM, request.nextUrl.pathname + request.nextUrl.search);

  return withCookies(NextResponse.redirect(signIn), clearedCookieWrites());
}

export async function middleware(request: NextRequest) {
  const onSignInPage = request.nextUrl.pathname === SIGN_IN_PATH;
  const accessToken = request.cookies.get(ACCESS_COOKIE)?.value;
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;

  // Someone already signed in is not sent on from the sign-in page here. Where they belong
  // depends on whether the account is staff or a customer, which only `GET /me` can say; the
  // page asks and redirects. A fixed redirect to the console would loop for a customer, whom the
  // console sends straight back to sign in.
  if (accessToken) {
    return NextResponse.next();
  }

  if (!refreshToken) {
    return onSignInPage ? NextResponse.next() : unauthenticated(request);
  }

  const tokens = await refreshSession(refreshToken).catch(() => null);

  if (!tokens) {
    return onSignInPage ? withCookies(NextResponse.next(), clearedCookieWrites()) : unauthenticated(request);
  }

  const writes = sessionCookieWrites(tokens);

  // The request that triggered the refresh still carries the old cookies. Rewrite its header so
  // the code behind this sees the new pair — otherwise this very request would render signed out.
  const forwarded = new Headers(request.headers);
  const cookies = new Map(request.cookies.getAll().map((cookie) => [cookie.name, cookie.value]));

  cookies.set(ACCESS_COOKIE, tokens.accessToken);
  cookies.set(REFRESH_COOKIE, tokens.refreshToken);

  forwarded.set(
    'cookie',
    [...cookies].map(([name, value]) => `${name}=${encodeURIComponent(value)}`).join('; '),
  );

  return withCookies(NextResponse.next({ request: { headers: forwarded } }), writes);
}

export const config = {
  // The staff console, its stream, the customer portal, and the sign-in page. The public site is
  // not matched: it has no session to keep and must not pay for a refresh it does not need.
  matcher: [
    '/login',
    '/queue/:path*',
    '/appointments/:path*',
    '/customers/:path*',
    '/pets/:path*',
    '/consultations/:path*',
    '/settings/:path*',
    '/schedules/:path*',
    '/account/:path*',
    '/billing/:path*',
    '/lab/:path*',
    '/dashboard/:path*',
    '/inventory/:path*',
    '/notifications/:path*',
    '/prescriptions/:path*',
    '/portal/:path*',
    '/api/queue/:path*',
  ],

  // In-memory refresh de-duplication (see refresh-session.ts) needs one long-lived module
  // instance, which the Node.js runtime gives and a per-request edge isolate does not.
  runtime: 'nodejs',
};

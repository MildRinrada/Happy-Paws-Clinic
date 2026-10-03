'use client';

import { useEffect, useState } from 'react';
import { SIGNED_IN_HINT_COOKIE } from './session-cookies';

/**
 * Whether this browser looks signed in, read from the hint cookie (see `session-cookies.ts`).
 *
 * False on the server and on the first paint, then corrected in the browser. The public pages are
 * prerendered for everyone, so they start out signed-out and switch after loading. It is a hint
 * for which link to show, never a check of who may do what.
 */
export function useSignedInHint(): boolean {
  const [signedIn, setSignedIn] = useState(false);

  useEffect(() => {
    setSignedIn(document.cookie.split('; ').some((pair) => pair === `${SIGNED_IN_HINT_COOKIE}=1`));
  }, []);

  return signedIn;
}

'use client';

import { useEffect, useState } from 'react';

/**
 * Reads the reset token from the address's fragment (`/reset-password#token=...`), once.
 *
 * The backend puts it in the fragment on purpose: browsers never send that part to a server, so
 * the token stays out of access logs and `Referer` headers. That also means only the browser can
 * read it, which is why this is a hook and not a page prop.
 *
 * Once read, it is wiped from the address bar, so it is not left on screen, in a screenshot or in
 * the browser history. Reloading the page afterwards therefore shows the "link not usable" message,
 * which offers a new link.
 *
 * Answers `undefined` before the fragment has been read, then the token or `null`.
 */
export function useLinkToken(): string | null | undefined {
  const [token, setToken] = useState<string | null | undefined>(undefined);

  useEffect(() => {
    const value = new URLSearchParams(window.location.hash.slice(1)).get('token');

    if (value) {
      window.history.replaceState(null, '', window.location.pathname + window.location.search);
    }

    setToken(value || null);
  }, []);

  return token;
}

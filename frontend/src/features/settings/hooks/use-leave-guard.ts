'use client';

import { useEffect } from 'react';

/** A plain left click on a link to another page of this site, opened in this tab. */
function leavingLink(event: MouseEvent): boolean {
  if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
    return false;
  }

  const anchor = event.target instanceof Element ? event.target.closest<HTMLAnchorElement>('a[href]') : null;

  if (!anchor || (anchor.target && anchor.target !== '_self') || anchor.hasAttribute('download')) {
    return false;
  }

  const url = new URL(anchor.href, window.location.href);

  // Another site is a full page load, which the browser's own "leave site?" already covers.
  return url.origin === window.location.origin && (url.pathname !== window.location.pathname || url.search !== window.location.search);
}

/**
 * While `active`, asks before the page is left: the browser's own question on reload, close or a
 * typed address, and `question` before a link inside the console is followed. The click is caught
 * on `window` before Next's `<Link>` sees it, so a "stay" stops the navigation outright.
 *
 * The browser's back and forward buttons are not asked about: the App Router has no way to hold
 * them, and the page they lead to is one the person has already seen.
 */
export function useLeaveGuard(active: boolean, question: string) {
  useEffect(() => {
    if (!active) return;

    const beforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      // Still needed by some browsers to show the question at all.
      event.returnValue = '';
    };

    const click = (event: MouseEvent) => {
      if (leavingLink(event) && !window.confirm(question)) {
        event.preventDefault();
        event.stopPropagation();
      }
    };

    window.addEventListener('beforeunload', beforeUnload);
    window.addEventListener('click', click, true);

    return () => {
      window.removeEventListener('beforeunload', beforeUnload);
      window.removeEventListener('click', click, true);
    };
  }, [active, question]);
}

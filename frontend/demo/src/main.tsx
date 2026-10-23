import './mock/install';
import './styles.css';
import './mock/demo-session';
import { createRoot } from 'react-dom/client';
import { DemoBar } from './demo-bar/demo-bar';
import { ensureSeeded } from './mock';
import RootLayout from './root-layout';
import { isNavigationError, isRedirectError, navigate, toPath } from './runtime/navigation';
import { Router } from './runtime/router';
import { shouldHandle } from './shims/next-link';

ensureSeeded();

// A plain `<a href="/…">` in the product is a full page load in Next; here it is the same
// navigation a Link makes, so the demo never reloads and loses nothing.
document.addEventListener('click', (event) => {
  const anchor = (event.target as Element | null)?.closest?.('a[href]') as HTMLAnchorElement | null;
  if (!anchor || anchor.hasAttribute('data-demo-link') || !shouldHandle(event, anchor)) return;
  const href = anchor.getAttribute('href') ?? '';
  if (href.startsWith('#') || href.startsWith('mailto:') || href.startsWith('tel:') || href.startsWith('data:')) return;
  const path = toPath(anchor.href);
  if (path === null || path.startsWith('/api/')) return;
  event.preventDefault();
  navigate(path);
});

// `redirect()` thrown where nothing waits on it (an event handler, a transition) still moves on.
window.addEventListener('unhandledrejection', (event) => {
  if (isRedirectError(event.reason)) {
    event.preventDefault();
    navigate(event.reason.url, { replace: event.reason.mode === 'replace' });
  }
});

// `redirect()` and `notFound()` are thrown on purpose and handled by the router's boundary; React
// would otherwise report each one as a caught error.
createRoot(document.getElementById('root')!, {
  onCaughtError(error, info) {
    if (isNavigationError(error)) return;
    console.error(error, info.componentStack);
  },
}).render(
  // No StrictMode: it runs effects twice in development, and the product's hooks that read a
  // one-time token out of the address (invitations, password reset) clear it on the first run.
  <>
    <DemoBar />
    <Router Root={RootLayout} />
  </>,
);

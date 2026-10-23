import type { AnchorHTMLAttributes, MouseEvent, ReactNode, Ref } from 'react';
import { navigate, toPath } from '@demo/runtime/navigation';

/** `next/link`: an ordinary link that the demo's router follows without reloading the page. */

type Href = string | { pathname?: string; query?: Record<string, string | number | undefined>; hash?: string };

interface LinkProps extends Omit<AnchorHTMLAttributes<HTMLAnchorElement>, 'href'> {
  href: Href;
  replace?: boolean;
  scroll?: boolean;
  prefetch?: boolean | null;
  children?: ReactNode;
  ref?: Ref<HTMLAnchorElement>;
}

function hrefOf(href: Href): string {
  if (typeof href === 'string') return href;
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(href.query ?? {})) {
    if (value !== undefined) query.set(key, String(value));
  }
  const search = query.size ? `?${query}` : '';
  return `${href.pathname ?? ''}${search}${href.hash ? `#${href.hash.replace(/^#/, '')}` : ''}`;
}

export function shouldHandle(event: MouseEvent<HTMLAnchorElement> | globalThis.MouseEvent, anchor: HTMLAnchorElement): boolean {
  return (
    !event.defaultPrevented &&
    event.button === 0 &&
    !event.metaKey &&
    !event.ctrlKey &&
    !event.shiftKey &&
    !event.altKey &&
    (!anchor.target || anchor.target === '_self') &&
    !anchor.hasAttribute('download')
  );
}

export default function Link({ href, replace, scroll, prefetch: _prefetch, onClick, children, ref, ...rest }: LinkProps) {
  const url = hrefOf(href);

  function handleClick(event: MouseEvent<HTMLAnchorElement>) {
    onClick?.(event);
    if (!shouldHandle(event, event.currentTarget) || toPath(url) === null) return;
    event.preventDefault();
    navigate(url, { replace, scroll });
  }

  return (
    <a {...rest} ref={ref} href={url} onClick={handleClick} data-demo-link="">
      {children}
    </a>
  );
}

/** Next shows a link as pending while its page loads; the demo's pages load in a moment, so never. */
export function useLinkStatus() {
  return { pending: false };
}

/* eslint-disable @next/next/no-img-element -- a local preview of the chosen file, the API's redirect to a signed link, or the static paw. */
import { clinicLogoText } from '../../../content/clinic-logo-settings';

interface BrowserTabPreviewProps {
  tabIconSrc?: string;
  logoSrc?: string;
  name: string;
}

/**
 * A browser tab at its real size: the tab icon when there is one, else the logo fitted into the
 * same 16 px square (as `/brand/tab-icon` draws it), else the paw.
 */
export function BrowserTabPreview({ tabIconSrc, logoSrc, name }: BrowserTabPreviewProps) {
  const text = clinicLogoText.preview;
  const src = tabIconSrc ?? logoSrc ?? '/icon.svg';

  return (
    <figure className="space-y-1.5">
      <figcaption className="text-xs text-ink-600">{text.tab}</figcaption>
      <div className="flex h-10 max-w-full items-end rounded-lg bg-ink-200 px-2 pt-2">
        <div className="flex h-8 w-60 min-w-0 items-center gap-2 rounded-t-lg bg-white px-3">
          <img src={src} alt="" className="size-4 shrink-0 object-contain" />
          <span className="min-w-0 flex-1 truncate text-xs text-ink-800">
            {text.tabTitle} · {name}
          </span>
          <span className="text-xs text-ink-400" aria-hidden="true">
            ✕
          </span>
        </div>
      </div>
    </figure>
  );
}

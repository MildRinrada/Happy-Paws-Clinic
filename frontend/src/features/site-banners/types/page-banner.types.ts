/** The public pages whose top banner comes through `loadPageBanner`. */
export type PageBannerPage = 'services' | 'team';

/** A button on a banner: its words and where it goes. */
export interface PageBannerAction {
  label: string;
  href: string;
}

/**
 * What a page's top banner says and shows. Today it is the defaults in
 * `content/page-banners.ts`; the owner wants it editable from the staff console later (a proposed
 * extension that awaits approval), when `lib/load-page-banner.ts` will read it from the API.
 */
export interface PageBannerData {
  eyebrow: string;
  /** A line break in it is kept on a wide screen. */
  heading: string;
  lead: string;
  primaryAction: PageBannerAction;
  /** A second, quieter button. Left out, the banner has one button. */
  secondaryAction?: PageBannerAction;
  /** Without it, an empty picture slot is drawn. */
  image?: { src: string; alt: string };
}

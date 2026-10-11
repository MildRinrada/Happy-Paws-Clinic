import { ImageResponse } from 'next/og';
import type { ImageBytes } from '@/lib/api/image-fetch';
import { getCachedClinicProfile } from '../api/getCachedClinicProfile';
import { logoSrcOf, tabIconSrcOf } from './brand-image-src';
import { imageDimensions } from './image-dimensions';
import { readBrandImage } from './read-brand-image';

/** The paw every page had before § 50, kept under /public. */
const FALLBACK_ICON = '/icon.svg';

/** A browser or phone may keep one answer this long, the same as the profile read behind it. */
const CACHE = 'public, max-age=300';

export interface BrandIconOptions {
  /** The square's side in pixels. */
  size: number;
  /** Fill behind the picture. A phone's home screen turns transparency black, so it gets white. */
  background?: string;
  /** The space left around the picture, as a share of the side. */
  padding?: number;
  /** Draw an uploaded tab icon into the square too, rather than sending the browser to it. */
  drawTabIcon?: boolean;
}

const redirect = (location: string) => new Response(null, { status: 307, headers: { Location: location, 'Cache-Control': CACHE } });

/**
 * The picture fitted, uncropped, in the middle of a square, as PNG bytes. PNG and JPEG only: the
 * renderer reads no WebP. Rendered to the end here rather than streamed, so a picture the renderer
 * cannot draw is caught and falls back instead of breaking the answer half way.
 */
async function squareOf(image: ImageBytes, { size, background, padding = 0 }: BrandIconOptions): Promise<Response | undefined> {
  const dimensions = imageDimensions(image.bytes);
  if (!dimensions || !['image/png', 'image/jpeg'].includes(image.contentType)) return undefined;

  const room = size * (1 - 2 * padding);
  const scale = room / Math.max(dimensions.width, dimensions.height);
  const source = `data:${image.contentType};base64,${Buffer.from(image.bytes).toString('base64')}`;

  try {
    const png = await new ImageResponse(
      (
        <div
          style={{
            width: '100%',
            height: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            ...(background ? { background } : {}),
          }}
        >
          {/* eslint-disable-next-line @next/next/no-img-element -- drawn by the image renderer, not the browser. */}
          <img
            src={source}
            alt=""
            width={Math.max(1, Math.round(dimensions.width * scale))}
            height={Math.max(1, Math.round(dimensions.height * scale))}
          />
        </div>
      ),
      { width: size, height: size },
    ).arrayBuffer();

    return new Response(png, { headers: { 'Content-Type': 'image/png', 'Cache-Control': CACHE } });
  } catch {
    return undefined;
  }
}

/**
 * The browser tab's icon, or a phone's home-screen icon (docs/contract-extensions.md § 50), in the
 * order the clinic's pictures allow: its square tab icon, else its logo fitted into a square, else
 * the paw. A wide logo sent to the browser as it is would be squeezed into 16 px, so it is drawn
 * into a square here. Anything that goes wrong (the API away, a picture that cannot be read) ends
 * at the paw rather than at no icon.
 */
export async function brandIconResponse(options: BrandIconOptions): Promise<Response> {
  try {
    const profile = await getCachedClinicProfile();
    const tabIcon = tabIconSrcOf(profile.tab_icon_url);
    const logo = logoSrcOf(profile.logo_url);

    if (tabIcon) {
      if (!options.drawTabIcon) return redirect(tabIcon);
      const image = await readBrandImage('tab-icon', profile.tab_icon_url);
      return (image && (await squareOf(image, options))) ?? redirect(tabIcon);
    }

    if (logo) {
      const image = await readBrandImage('logo', profile.logo_url);
      return (image && (await squareOf(image, options))) ?? redirect(logo);
    }
  } catch {
    // Falls through to the paw.
  }

  return redirect(FALLBACK_ICON);
}

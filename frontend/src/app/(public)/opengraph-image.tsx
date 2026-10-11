import { readFile } from 'node:fs/promises';
import { join } from 'node:path';
import { ImageResponse } from 'next/og';
import { ogImage } from '@/features/seo/content/seo';

/**
 * The picture shown when a public page is shared (LINE, Facebook, X). Drawn once at build time,
 * so the font files are read then and not needed by the running server.
 *
 * The built-in font has no Thai letters, so it uses Prompt (the site's heading font, SIL Open
 * Font License, see src/assets/fonts/OFL.txt). The words are in features/seo/content/seo.ts.
 */
export const alt = ogImage.alt;
export const size = { width: ogImage.width, height: ogImage.height };
export const contentType = 'image/png';

const font = (file: string) => readFile(join(process.cwd(), 'src/assets/fonts', file));

export default async function OpenGraphImage() {
  const [semiBold, regular] = await Promise.all([font('Prompt-SemiBold.ttf'), font('Prompt-Regular.ttf')]);

  return new ImageResponse(
    (
      <div
        style={{
          width: '100%',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
          padding: '72px 80px',
          background: '#13322b',
          color: '#ffffff',
          fontFamily: 'Prompt',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
          {/* The same paw as the browser tab icon (public/icon.svg). */}
          <svg width="88" height="88" viewBox="0 0 32 32">
            <rect width="32" height="32" rx="7" fill="#217a71" />
            <g fill="#ffffff">
              <ellipse cx="9.2" cy="14.2" rx="2.3" ry="3" />
              <ellipse cx="13.4" cy="9.4" rx="2.3" ry="3.1" />
              <ellipse cx="18.6" cy="9.4" rx="2.3" ry="3.1" />
              <ellipse cx="22.8" cy="14.2" rx="2.3" ry="3" />
              <path d="M16 14.6c-3.3 0-6.4 3.7-6.4 6.7 0 2 1.5 3.1 3.3 3.1 1.2 0 2-.6 3.1-.6s1.9.6 3.1.6c1.8 0 3.3-1.1 3.3-3.1 0-3-3.1-6.7-6.4-6.7z" />
            </g>
          </svg>
          <div style={{ fontSize: 44, fontWeight: 600, color: '#ff9500' }}>{ogImage.eyebrow}</div>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', fontSize: 92, fontWeight: 600, lineHeight: 1.3 }}>
          {ogImage.headingLines.map((line) => (
            <div key={line}>{line}</div>
          ))}
        </div>

        <div style={{ display: 'flex' }}>
          <div
            style={{
              display: 'flex',
              padding: '14px 32px',
              borderRadius: 16,
              background: '#ff9500',
              fontSize: 38,
              fontWeight: 400,
            }}
          >
            {ogImage.footer}
          </div>
        </div>
      </div>
    ),
    {
      ...size,
      fonts: [
        { name: 'Prompt', data: semiBold, weight: 600, style: 'normal' },
        { name: 'Prompt', data: regular, weight: 400, style: 'normal' },
      ],
    },
  );
}

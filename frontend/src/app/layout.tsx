import type { Metadata } from 'next';
import { IBM_Plex_Sans_Thai, Inter, Prompt } from 'next/font/google';
import type { ReactNode } from 'react';
import './globals.css';

// Self-hosted at build time by next/font, so the console makes no request to a font CDN from
// inside a clinic's network and text never flashes in a fallback face.
const plexThai = IBM_Plex_Sans_Thai({
  subsets: ['thai', 'latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-plex-thai',
  display: 'swap',
});

const prompt = Prompt({
  subsets: ['thai', 'latin'],
  // 300/400 are the public site's body text; the console only uses Prompt for headings.
  weight: ['300', '400', '500', '600', '700'],
  variable: '--font-prompt',
  display: 'swap',
});

// One weight, Latin only: it sets the wordmark beside the logo and nothing else.
const inter = Inter({
  subsets: ['latin'],
  weight: ['700'],
  variable: '--font-inter',
  display: 'swap',
});

export const metadata: Metadata = {
  title: 'Happy Paws Clinic',
  description: 'ระบบจัดการคลินิกสัตวแพทย์',
};

/**
 * Root layout shared by every route group. Group-specific chrome — the staff console shell,
 * the customer portal shell, the LIFF shell — belongs in each group's own layout.tsx, not here.
 */
export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    // data-scroll-behavior: globals.css scrolls smoothly for in-page anchors; this tells Next to
    // switch it off while changing route, so a new page opens at its top rather than gliding there.
    <html
      lang="th"
      data-scroll-behavior="smooth"
      className={`${plexThai.variable} ${prompt.variable} ${inter.variable}`}
    >
      <body>{children}</body>
    </html>
  );
}

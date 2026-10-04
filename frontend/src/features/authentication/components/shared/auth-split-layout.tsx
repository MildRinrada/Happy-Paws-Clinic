import type { ReactNode } from 'react';
import { MobileBrand } from '../sign-in/mobile-brand';
import { SignInShowcase } from '../sign-in/sign-in-showcase';

/**
 * The sign-in page's frame, shared by sign-up and the password pages: the photo slideshow on the
 * left from laptop width up, the form column on the right, and a small logo above the form on a
 * phone where the slideshow is hidden.
 */
export function AuthSplitLayout({ children }: { children: ReactNode }) {
  return (
    <main className="grid min-h-dvh bg-white lg:grid-cols-2">
      <SignInShowcase />

      <section className="flex min-w-0 items-center justify-center px-4 py-12 sm:px-8">
        <div className="w-full min-w-0 max-w-[380px]">
          <MobileBrand />
          {children}
        </div>
      </section>
    </main>
  );
}

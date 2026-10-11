import type { MetadataRoute } from 'next';
import { absoluteUrl } from '@/features/seo/lib/absolute-url';

/**
 * Areas search engines have no business in: everything behind a sign-in (the staff console, the
 * customer portal), the sign-in pages themselves, the queue display, and the app's own API
 * routes. They all need an account anyway; this keeps crawlers from wasting time on redirects.
 * Keep the list in step with the route folders under src/app.
 */
const PRIVATE_PATHS = [
  // sign-in and sign-up
  '/login',
  '/register',
  '/forgot-password',
  '/reset-password',
  '/invitations',
  // customer portal
  '/portal',
  // staff console and the queue display screen
  '/account',
  '/appointments',
  '/billing',
  '/consultations',
  '/crm',
  '/customers',
  '/dashboard',
  '/inventory',
  '/lab',
  '/messages',
  '/notifications',
  '/pets',
  '/prescriptions',
  '/queue',
  '/reports',
  '/schedules',
  '/settings',
  // the waiting-room screen that runs on its own token (docs/contract-extensions.md § 30)
  '/display',
  // route handlers
  '/api/',
];

/** `/robots.txt`: the public site is open to crawl, the rest is not, and here is the sitemap. */
export default function robots(): MetadataRoute.Robots {
  return {
    rules: { userAgent: '*', allow: '/', disallow: PRIVATE_PATHS },
    sitemap: absoluteUrl('/sitemap.xml'),
  };
}

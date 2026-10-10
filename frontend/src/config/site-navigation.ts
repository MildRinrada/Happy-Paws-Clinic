/**
 * The public site's header links. Edit the menu here, not in the header components.
 *
 * A menu link goes either to a page of its own or to a section of the home page (the `id` on that
 * section). Sign-up goes to the customer sign-up page.
 */
export interface SiteLink {
  label: string;
  href: string;
}

export const mainNavLinks: SiteLink[] = [
  { label: 'บริการ', href: '/services' },
  { label: 'ทีมของเรา', href: '/team' },
  { label: 'เกี่ยวกับคลินิก', href: '/about' },
  { label: 'บทความ', href: '/articles' },
  { label: 'รีวิว', href: '/#reviews' },
  { label: 'ติดต่อ', href: '/contact' },
];

export const signInLink: SiteLink = { label: 'เข้าสู่ระบบ', href: '/login' };

export const signUpLink: SiteLink = { label: 'สมัครใช้งานฟรี', href: '/register' };

/**
 * Shown instead of sign-in and sign-up to someone already signed in. It goes to the sign-in page
 * on purpose: that page sends a customer to the portal and a member of staff to the console, so one
 * link serves both.
 */
export const accountLink: SiteLink = { label: 'บัญชีของฉัน', href: '/login' };

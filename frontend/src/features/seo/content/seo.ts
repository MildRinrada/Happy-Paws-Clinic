/**
 * What search engines and link previews say about the public site. Each page's own title and
 * description live with that page's content; these are the parts every page shares.
 *
 * Nothing here is a clinic fact. The name, address, phone, hours and prices in structured data
 * come from the API, so they follow the clinic's own settings.
 */
export const seoSite = {
  /** The brand in every page title and in link previews, and the fallback clinic name. */
  name: 'Happy Paws Clinic',
  locale: 'th_TH',
  language: 'th',
  /** Breadcrumbs start here. */
  homeLabel: 'หน้าแรก',
};

/** The home page's own title and description. */
export const homeMeta = {
  title: 'Happy Paws Clinic | คลินิกสัตว์เล็ก ดูแลสุนัขและแมว',
  description:
    'คลินิกสัตว์เล็กย่านสุขุมวิท ตรวจสุขภาพ วัคซีน ทันตกรรม ผ่าตัด แล็บ และกรูมมิ่ง ดูบริการและราคา แล้วจองนัดออนไลน์ได้',
};

/** The picture shown when a page is shared on LINE, Facebook or X. Drawn by `opengraph-image.tsx`. */
export const ogImage = {
  path: '/opengraph-image',
  width: 1200,
  height: 630,
  alt: 'Happy Paws Clinic คลินิกสัตว์เล็ก ดูแลสุนัขและแมว',
  eyebrow: 'Happy Paws Clinic',
  headingLines: ['คลินิกสัตว์เล็ก', 'ดูแลสุนัขและแมว'],
  footer: 'ดูบริการและราคา จองนัดออนไลน์',
};

/** Each inner page's name and path, for breadcrumbs and the sitemap. */
export const sitePages = {
  services: { name: 'บริการและราคา', path: '/services' },
  vaccineSchedule: { name: 'ตารางวัคซีน', path: '/vaccine-schedule' },
  about: { name: 'เกี่ยวกับคลินิก', path: '/about' },
  team: { name: 'ทีมของเรา', path: '/team' },
  articles: { name: 'บทความ', path: '/articles' },
  reviews: { name: 'รีวิวจากลูกค้า', path: '/reviews' },
  contact: { name: 'ติดต่อเรา', path: '/contact' },
  emergency: { name: 'กรณีฉุกเฉิน', path: '/emergency' },
  privacy: { name: 'นโยบายความเป็นส่วนตัว', path: '/privacy' },
};

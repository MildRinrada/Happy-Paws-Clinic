import type { PageBannerData, PageBannerPage } from '../types/page-banner.types';

/**
 * What each page's top banner says and shows, one entry per page. These are the defaults
 * `lib/load-page-banner.ts` returns until the banners can be edited in the staff console. Edit the
 * wording, the buttons and the pictures here.
 */
export const pageBannerDefaults: Record<PageBannerPage, PageBannerData> = {
  services: {
    eyebrow: 'บริการและราคา',
    heading: 'รู้ราคาก่อนมา เลือกบริการ\nแล้วจองได้เลย',
    lead: 'ทุกบริการที่จองออนไลน์ได้ พร้อมราคาและเวลาที่ใช้ กดจองที่บริการไหนก็ได้ ระบบจะเลือกบริการนั้นไว้ให้',
    primaryAction: { label: 'จองนัดหมาย', href: '/book' },
    secondaryAction: { label: 'ดูช่องทางติดต่อ', href: '/contact' },
    image: { src: '/images/about/vet-exam-dog.jpg', alt: 'สัตวแพทย์ประคองสุนัขบนโต๊ะตรวจ' },
  },
  team: {
    eyebrow: 'ทีมของเรา',
    heading: 'คนที่ดูแลน้องของคุณ\nตั้งแต่เคาน์เตอร์จนถึงวันกลับบ้าน',
    lead: 'ทุกคนที่ดูแลน้องของคุณ ตั้งแต่เคาน์เตอร์ต้อนรับ ห้องตรวจ จนถึงวันที่น้องได้กลับบ้าน',
    primaryAction: { label: 'จองนัดกับสัตวแพทย์', href: '/book' },
    // No picture of the whole team yet: the banner draws an empty slot that says what belongs there.
  },
};

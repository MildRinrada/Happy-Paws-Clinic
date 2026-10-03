import type { SiteLink } from '@/config/site-navigation';

// The vets themselves come from the API (GET /team?department=veterinary), the same people as the
// team page. Change who is shown, their title or their photo in each person's public profile.

export const vetsHeading = 'คนที่จะดูแลน้องของคุณ';

/** How many vets the home page shows, most experienced first. */
export const homeVetCount = 4;

/** The link on each card to that person on the team page. */
export const vetProfileLabel = 'ดูโปรไฟล์';

/** The link beside the heading to the whole team. */
export const allTeamLink: SiteLink = { label: 'ดูทีมทั้งหมด', href: '/team' };

export const vetsUnavailable = 'ยังโหลดรายชื่อสัตวแพทย์ไม่ได้ ลองใหม่อีกครั้งภายหลัง';

import { STAFF } from '@demo/mock/modules/clinic/seed';
import { teamBannerUrl } from '@demo/mock/modules/clinic/portraits';

/**
 * The page banners as the product has them, with the team page's picture filled in: a drawing of
 * the sample staff in front of the clinic, in the same hand as their portraits on the cards below.
 */
export * from '@/features/site-banners/content/page-banners.ts?original';
import { pageBannerDefaults as product } from '@/features/site-banners/content/page-banners.ts?original';

const team = STAFF.filter((member) => member.profile.show_on_website).map((member) => ({
  display_name: member.name,
  department: member.department,
}));

export const pageBannerDefaults: typeof product = {
  ...product,
  team: { ...product.team, image: { src: teamBannerUrl(team), alt: 'ภาพวาดทีมงาน Happy Paws Clinic หน้าคลินิก' } },
};

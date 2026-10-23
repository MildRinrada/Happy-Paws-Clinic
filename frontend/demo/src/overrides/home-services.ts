import type { FeaturedService, MoreService } from '@/features/home/types/home.types';
import {
  featuredServices as productFeatured,
  moreServices as productMore,
} from '@/features/home/content/services.ts?original';
import checkupPhoto from './images/service-checkup.jpg';
import dentalPhoto from './images/service-dental.jpg';

/**
 * The home page's service cards with their pictures, which the product leaves for each clinic to
 * supply. Two wording slips in the product's copy are put right here too: the check-up card had the
 * dental card's lines and fasting tag, and the lab line gave its starting price twice.
 */
export * from '@/features/home/content/services.ts?original';

const pictures: Record<string, string> = {
  checkup: checkupPhoto,
  vaccine: '/images/sign-in/vet-injection-dog.jpg',
  dental: dentalPhoto,
};

export const featuredServices: FeaturedService[] = productFeatured.map((service) => {
  const withPicture = { ...service, image: { ...service.image, src: pictures[service.id] } };
  if (service.id !== 'checkup') return withPicture;
  return {
    ...withPicture,
    description: ['ตรวจร่างกายครบทุกระบบ ฟังหัวใจและปอด ตา หู ผิวหนัง', 'ชั่งน้ำหนักและแนะนำการดูแลตามวัย'],
    tags: [
      { label: '30 นาที', tone: 'neutral' },
      { label: 'ไม่ต้องงดอาหาร', tone: 'notice' },
    ],
  };
});

export const moreServices: MoreService[] = productMore.map((service) =>
  service.id === 'lab' ? { ...service, detail: 'รู้ผลใน 30 นาที · เริ่ม ฿800' } : service,
);

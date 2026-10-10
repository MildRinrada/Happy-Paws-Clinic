import type { PublicService } from '@/features/service-catalog/types/service.types';
import { nextFreeTime } from '../content/next-free-time';

/**
 * The service the "ว่างเร็วสุด" block shows times for, from the public menu (which holds only
 * services open for online booking): the one named in the content file, else the first of its
 * category, else the first on the menu. Nothing when the clinic offers nothing online.
 */
export function featuredService(services: PublicService[]): PublicService | undefined {
  const { code, category } = nextFreeTime.featuredService;

  return (
    services.find((service) => service.code === code) ??
    services.find((service) => service.category_code === category) ??
    services[0]
  );
}

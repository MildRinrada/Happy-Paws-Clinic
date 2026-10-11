import type { PublicService } from './service.types';

/**
 * One category on the public menu, with its services, in the order the contract lists categories.
 * `services` is empty for a category with nothing open for online booking.
 */
export interface MenuCategory {
  code: string;
  name: string;
  services: PublicService[];
}

/** The public "บริการและราคา" page's data. */
export interface ServiceMenu {
  categories: MenuCategory[];
  /** Branch names by id, to say which branches a service is at. Empty with one branch or none. */
  branchNames: Record<string, string>;
}

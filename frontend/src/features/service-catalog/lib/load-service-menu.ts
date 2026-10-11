import { cache } from 'react';
import { listBranches } from '@/features/clinic/api/listBranches';
import { listServices } from '../api/listServices';
import { listServiceCategories } from '../api/manage-services';
import type { ServiceMenu } from '../types/service-menu.types';

/**
 * The public menu (`GET /services`) under every one of the contract's categories
 * (`GET /services/categories`), in their order, or `null` when the services cannot be read.
 *
 * A category with nothing open for online booking is kept, with no services, so the page can say
 * to call about it. A service whose category is not in the list goes at the end under its code.
 * If the categories cannot be read, only the categories the services name are shown, headed by
 * their codes. Branch names are an extra too: unread, branches are not named.
 *
 * Cached per request: the page and its structured data both read it.
 */
export const loadServiceMenu = cache(async (): Promise<ServiceMenu | null> => {
  const [services, categories, branches] = await Promise.all([
    listServices().catch(() => null),
    listServiceCategories().catch(() => []),
    listBranches().catch(() => []),
  ]);

  if (services === null) {
    return null;
  }

  const listed = categories.map((category) => category.code);
  const unlisted = [...new Set(services.map((service) => service.category_code))].filter((code) => !listed.includes(code));

  return {
    categories: [...listed, ...unlisted].map((code) => ({
      code,
      name: categories.find((category) => category.code === code)?.name ?? code,
      // In the order the API lists them.
      services: services.filter((service) => service.category_code === code),
    })),
    // With one branch, every service is at it, so there is nothing to say.
    branchNames: branches.length > 1 ? Object.fromEntries(branches.map((branch) => [branch.id, branch.name])) : {},
  };
});

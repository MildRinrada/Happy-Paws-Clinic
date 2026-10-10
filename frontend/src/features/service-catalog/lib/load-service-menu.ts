import { cache } from 'react';
import { listBranches } from '@/features/clinic/api/listBranches';
import { listServices } from '../api/listServices';
import { listServiceCategories } from '../api/manage-services';
import type { ServiceMenu } from '../types/service-menu.types';

/**
 * The public menu (`GET /services`) grouped under the contract's categories, in their order, or
 * `null` when the services cannot be read. The category names and branch names are extras: if
 * they cannot be read, a category is headed by its code and branches are not named.
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

  const order = categories.map((category) => category.code);
  const codes = [...new Set(services.map((service) => service.category_code))].sort(
    (a, b) => (order.indexOf(a) + 1 || order.length + 1) - (order.indexOf(b) + 1 || order.length + 1),
  );

  return {
    categories: codes.map((code) => ({
      code,
      name: categories.find((category) => category.code === code)?.name ?? code,
      // In the order the API lists them.
      services: services.filter((service) => service.category_code === code),
    })),
    // With one branch, every service is at it, so there is nothing to say.
    branchNames: branches.length > 1 ? Object.fromEntries(branches.map((branch) => [branch.id, branch.name])) : {},
  };
});

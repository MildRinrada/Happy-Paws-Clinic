import { listInventoryItems } from '@/features/inventory/api/listInventoryItems';
import { listServices } from '@/features/service-catalog/api/listServices';
import { listManagedServices } from '@/features/service-catalog/api/manage-services';
import { formatBaht } from '@/lib/format/money';
import { createInvoicePage } from '../content/create-invoice-page';
import type { CatalogueEntry } from './catalogue-entry';

interface CatalogueAccess {
  branchId: string;
  canManageServices: boolean;
  canReadInventory: boolean;
}

async function loadServices(branchId: string, canManage: boolean) {
  if (canManage) {
    const managed = await listManagedServices().catch(() => null);
    if (managed) {
      return managed
        .filter((service) => service.is_active && (service.branch_ids.length === 0 || service.branch_ids.includes(branchId)))
        .map(({ id, name, price_satang }) => ({ id, name, price_satang }));
    }
  }

  // The public menu holds only services open for online booking. It is what everyone may read.
  return (await listServices(branchId).catch(() => [])).map(({ id, name, price_satang }) => ({ id, name, price_satang }));
}

/**
 * What the line picker on `/billing/new` offers: the branch's services, and its medicines and
 * supplies when the session may read the stock room. Each read failing leaves its group empty
 * rather than costing the page: a line can always be typed by hand.
 *
 * Services come from `GET /staff/services` under `service:manage` (counter-only services
 * included), otherwise from the public menu. Items come from `GET /staff/inventory/items` under
 * `inventory:read`. A medicine line raised here is a charge only: it does not move stock.
 */
export async function loadCatalogue({ branchId, canManageServices, canReadInventory }: CatalogueAccess): Promise<CatalogueEntry[]> {
  const text = createInvoicePage.lines;
  const [services, items] = await Promise.all([
    loadServices(branchId, canManageServices),
    canReadInventory ? listInventoryItems({ branch: branchId }).catch(() => []) : Promise.resolve([]),
  ]);

  return [
    ...services.map<CatalogueEntry>((service) => ({
      value: `service:${service.id}`,
      group: 'service',
      label: text.serviceOption(service.name, formatBaht(service.price_satang)),
      description: service.name,
      kind: 'service',
      unitPriceSatang: service.price_satang,
    })),
    ...items.map<CatalogueEntry>((item) => ({
      value: `item:${item.id}`,
      group: 'item',
      label: text.itemOption(item.name, formatBaht(item.unit_price_satang), item.available, item.unit),
      description: item.name,
      kind: item.kind === 'medicine' ? 'medicine' : 'other',
      unitPriceSatang: item.unit_price_satang,
    })),
  ];
}

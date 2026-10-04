import { listServices } from '@/features/service-catalog/api/listServices';
import { authed } from '@/lib/auth/session';
import { clinicDay } from '@/lib/format/datetime';
import { listMyPackages } from '../api/listMyPackages';
import type { PackageBalance, PackageStatus } from '../types/package.types';
import { loadPortalHome } from './load-portal-home';

/** One package on the page, with the pet's and the services' names already looked up. */
export interface PackageView {
  id: string;
  name: string;
  status: PackageStatus;
  remaining: number;
  total: number;
  /** Absent when any pet on the account may use it. */
  petName?: string;
  /** The services it covers that the public menu names. Empty when none could be named. */
  serviceNames: string[];
  expiresOn?: string;
  /** Still usable and runs out within `SOON_DAYS`. */
  expiresSoon: boolean;
  purchasedAt: string;
}

const SOON_DAYS = 30;

/** Service id to name from the public menu. A miss, or a failed read, just leaves the names out. */
async function serviceNameMap(packages: PackageBalance[]): Promise<Map<string, string>> {
  if (!packages.some((item) => item.service_ids.length > 0)) return new Map();

  try {
    return new Map((await listServices()).map((service) => [service.id, service.name]));
  } catch {
    return new Map();
  }
}

/** The owner's packages, usable first and soonest to expire first, as the backend orders them. */
export async function loadPackages(): Promise<PackageView[]> {
  const [packages, home] = await Promise.all([authed(listMyPackages), loadPortalHome()]);
  const services = await serviceNameMap(packages);
  const pets = new Map(home.pets.map((pet) => [pet.id, pet.name]));
  const soon = clinicDay(new Date(Date.now() + SOON_DAYS * 24 * 60 * 60 * 1000));

  return packages.map((item) => ({
    id: item.id,
    name: item.name,
    status: item.status,
    remaining: item.remaining,
    total: item.total_uses,
    petName: item.pet_id ? pets.get(item.pet_id) : undefined,
    serviceNames: item.service_ids.map((id) => services.get(id)).filter((name): name is string => Boolean(name)),
    expiresOn: item.expires_on ?? undefined,
    expiresSoon: item.status === 'active' && Boolean(item.expires_on && item.expires_on <= soon),
    purchasedAt: item.purchased_at,
  }));
}

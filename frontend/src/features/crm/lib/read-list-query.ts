import type { CouponStatus, PackageStatus } from '../types/crm.types';

type SearchParams = Record<string, string | string[] | undefined>;

const COUPON_STATUSES: CouponStatus[] = ['active', 'scheduled', 'used_up', 'expired', 'deactivated'];
const PACKAGE_STATUSES: PackageStatus[] = ['pending_payment', 'active', 'used_up', 'expired', 'cancelled'];

function text(params: SearchParams, key: string): string | undefined {
  const value = params[key];
  return typeof value === 'string' && value.trim() ? value.trim() : undefined;
}

/** `?q=&status=&cursor=` of the coupon list. An unknown status is dropped rather than sent. */
export function readCouponQuery(params: SearchParams) {
  const status = text(params, 'status');

  return {
    q: text(params, 'q'),
    status: COUPON_STATUSES.find((value) => value === status),
    cursor: text(params, 'cursor'),
  };
}

/** `?customer_id=&status=` of the package lookup. */
export function readPackageQuery(params: SearchParams) {
  const status = text(params, 'status');

  return {
    customerId: text(params, 'customer_id') ?? '',
    status: PACKAGE_STATUSES.find((value) => value === status),
  };
}

/** `?cursor=` of a paged list. */
export function readCursor(params: SearchParams): string | undefined {
  return text(params, 'cursor');
}

/** The same query string with another cursor, or without one. */
export function withCursor(path: string, query: Record<string, string | undefined>, cursor?: string): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries({ ...query, cursor })) {
    if (value) search.set(key, value);
  }

  const rendered = search.toString();
  return rendered ? `${path}?${rendered}` : path;
}

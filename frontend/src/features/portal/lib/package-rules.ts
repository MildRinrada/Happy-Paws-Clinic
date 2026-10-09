import type { PackageStatus } from '../types/package.types';

/** Usable now, or once its bill is paid: the top group of the page. The rest are finished. */
export function isUsablePackage(status: PackageStatus): boolean {
  return status === 'active' || status === 'pending_payment';
}

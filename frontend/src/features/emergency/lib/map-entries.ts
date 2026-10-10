import type { SiteInfo } from '@/features/clinic/lib/load-site-info';
import type { ClinicContact } from '@/features/clinic/types/contact.types';

export interface MapEntry {
  map: ClinicContact;
  /** The branch's name, when the clinic has several and the map is one branch's. */
  title?: string;
}

/** The clinic's map entries for the directions section: the clinic-wide ones first, then each branch's. */
export function mapEntries(info: SiteInfo): MapEntry[] {
  const maps = info.contacts.filter((contact) => contact.kind === 'map');
  const wide = maps.filter((map) => !map.branch_id).map((map) => ({ map }));
  const byBranch = info.branches.flatMap((branch) =>
    maps
      .filter((map) => map.branch_id === branch.id)
      .map((map) => ({ map, title: info.branches.length > 1 ? branch.name : undefined })),
  );

  return [...wide, ...byBranch];
}

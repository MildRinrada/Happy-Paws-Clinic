import { loadPortalHome } from './load-portal-home';

export type PetVaccineStatus = 'overdue' | 'due_soon' | 'given';

/** One vaccine on a pet's page. `date` is `YYYY-MM-DD`: the due day, or the day it was given. */
export interface PetVaccine {
  name: string;
  status: PetVaccineStatus;
  date: string;
}

const ORDER: Record<PetVaccineStatus, number> = { overdue: 0, due_soon: 1, given: 2 };

/**
 * A pet's vaccines, most urgent first.
 *
 * For now these are only the ones overdue or due within 30 days, from `vaccinations_due` on
 * `GET /portal/home`, which the layout reads anyway. `complete: false` says so, and the page adds a
 * line that the vaccines already given will appear later. A full list (given doses included) is
 * proposed as ส่วนขยายข้อ 44, not approved yet; when it exists, only this function changes and
 * returns `complete: true`. No "ฉีดแล้ว" row is ever made up.
 */
export async function loadPetVaccines(petId: string): Promise<{ items: PetVaccine[]; complete: boolean }> {
  const home = await loadPortalHome();

  const items = home.vaccinations_due
    .filter((item) => item.pet_id === petId)
    .map((item): PetVaccine => ({ name: item.vaccine_name, status: item.status, date: item.next_due_on }))
    .sort((a, b) => ORDER[a.status] - ORDER[b.status] || a.date.localeCompare(b.date));

  return { items, complete: false };
}

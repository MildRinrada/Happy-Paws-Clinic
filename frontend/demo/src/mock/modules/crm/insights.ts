import { day, nowIso, table, type Row } from '../../core/db';
import { packagesOfCustomer, tierOfCustomer } from './loyalty';
import { canReceiveLine } from './notify';
import {
  activeCustomers,
  customer,
  daysBetween,
  hasMarketingConsent,
  hash,
  homeBranchOf,
  petsOf,
  rng,
  spendOf,
  visitDaysOf,
} from './store';

/**
 * What the CRM works out about customers: vaccine due dates, visit rhythm, segment membership and
 * the due-back list. Vaccine doses and visits belong to other modules; when their tables have
 * nothing yet for a pet or a customer, a small history kept in this module's own tables stands in,
 * so the CRM pages are never empty while the rest of the demo is still being filled.
 */

export interface VaccineDue {
  pet_id: string;
  customer_id: string;
  vaccine_name: string;
  due_on: string;
  last_given_on?: string;
}

const DOSE_TABLES = ['vaccinations', 'pet_vaccinations', 'vaccination_doses', 'vaccines_given'];

const DOG_VACCINES = ['วัคซีนรวม 5 โรค (DHPPL)', 'วัคซีนพิษสุนัขบ้า', 'วัคซีนไข้หวัดสุนัข (Kennel cough)'];
const CAT_VACCINES = ['วัคซีนรวมแมว (FVRCP)', 'วัคซีนพิษสุนัขบ้า', 'วัคซีนลิวคีเมียแมว (FeLV)'];

function isDog(species: string): boolean {
  return /สุนัข|หมา|dog/i.test(species);
}
function isCat(species: string): boolean {
  return /แมว|cat/i.test(species);
}

/** The latest dose of each vaccine per pet, from whichever module keeps doses. */
function dosesFromOtherModules(): VaccineDue[] {
  const latest = new Map<string, VaccineDue>();
  for (const name of DOSE_TABLES) {
    for (const dose of table(name).all()) {
      const due = dose.next_due_on;
      const petId = dose.pet_id;
      if (!due || !petId) continue;
      const owner = table('pets').get(petId)?.primary_customer_id;
      if (!owner) continue;
      const key = `${petId}|${dose.vaccine_name}`;
      const given = String(dose.administered_at ?? '').slice(0, 10);
      const current = latest.get(key);
      if (!current || (current.last_given_on ?? '') < given) {
        latest.set(key, { pet_id: petId, customer_id: owner, vaccine_name: dose.vaccine_name, due_on: due, last_given_on: given || undefined });
      }
    }
  }
  return [...latest.values()];
}

/**
 * Stand-in due dates for pets the doses tables say nothing about: one or two vaccines each, spread
 * from two months overdue to two months ahead. Kept, so they stay put for the day.
 */
function standInDues(covered: Set<string>): VaccineDue[] {
  const store = table('crm_vaccine_standins');
  const out: VaccineDue[] = [];
  for (const pet of table('pets').where((p) => !p.is_deceased)) {
    if (covered.has(pet.id)) continue;
    let rows = store.where((r) => r.pet_id === pet.id);
    if (rows.length === 0) {
      const random = rng(`vac-${pet.id}`);
      const list = isDog(pet.species ?? '') ? DOG_VACCINES : isCat(pet.species ?? '') ? CAT_VACCINES : [];
      const count = list.length ? 1 + Math.floor(random() * 2) : 0;
      const picked = [...list].sort(() => random() - 0.5).slice(0, count);
      rows = store.insertMany(
        picked.map((vaccine_name) => {
          const offset = Math.round(random() * 130) - 70;
          return { pet_id: pet.id, customer_id: pet.primary_customer_id, vaccine_name, due_on: day(offset), last_given_on: day(offset - 365) };
        }),
      );
    }
    out.push(...rows.map(({ id: _id, ...r }) => r as unknown as VaccineDue));
  }
  return out;
}

export function vaccineDues(): VaccineDue[] {
  const real = dosesFromOtherModules();
  const covered = new Set(real.map((r) => r.pet_id));
  return [...real, ...standInDues(covered)].filter((d) => {
    const pet = table('pets').get(d.pet_id);
    return pet && !pet.is_deceased;
  });
}

/**
 * A customer's visit days. The appointments and bills of the other modules when they show at least
 * two visits; otherwise a stand-in rhythm (every one to four months) kept in this module.
 */
export function visitsOf(customerId: string): string[] {
  const real = visitDaysOf(customerId);
  if (real.length >= 2) return real;
  const store = table('crm_visit_standins');
  let row = store.find((r) => r.customer_id === customerId);
  if (!row) {
    const random = rng(`visits-${customerId}`);
    const count = 2 + Math.floor(random() * 5);
    const gap = 30 + Math.floor(random() * 90);
    // Most come back on time; about one in four is late by a few weeks to a few months.
    const late = random() < 0.28 ? Math.floor(gap * (0.8 + random() * 1.5)) : -Math.floor(random() * gap * 0.8);
    const last = -Math.max(1, Math.min(400, gap + late));
    const days = Array.from({ length: count }, (_, i) => day(last - i * gap - Math.floor(random() * 10))).reverse();
    row = store.insert({ customer_id: customerId, days });
  }
  return [...new Set([...(row.days as string[]), ...real])].sort();
}

export function lastVisitOf(customerId: string): string | undefined {
  const visits = visitsOf(customerId);
  return visits[visits.length - 1];
}

function median(values: number[]): number {
  const sorted = [...values].sort((a, b) => a - b);
  const mid = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[mid]! : Math.round((sorted[mid - 1]! + sorted[mid]!) / 2);
}

/** Whether the customer has a visit booked from now on. */
export function hasUpcomingVisit(customerId: string): boolean {
  return Boolean(
    table('appointments').find((a) => a.customer_id === customerId && a.starts_at > nowIso() && ['booked', 'confirmed'].includes(a.status)),
  );
}

/** A customer later back than their own usual gap (docs/contract-extensions.md § 33), or undefined. */
export function lapseOf(customerId: string) {
  const visits = visitsOf(customerId);
  if (visits.length < 2) return undefined;
  const gaps = visits.slice(1).map((d, i) => daysBetween(visits[i]!, d)).filter((g) => g > 0);
  if (gaps.length === 0) return undefined;
  const usual = median(gaps);
  const allowed = Math.max(30, Math.round(usual * 1.5));
  const last = visits[visits.length - 1]!;
  const dueOn = new Date(`${last}T00:00:00`);
  dueOn.setDate(dueOn.getDate() + allowed);
  const due = `${dueOn.getFullYear()}-${String(dueOn.getMonth() + 1).padStart(2, '0')}-${String(dueOn.getDate()).padStart(2, '0')}`;
  const overdue = daysBetween(due, day(0));
  if (overdue <= 0) return undefined;
  // Someone with a visit already booked is not lapsed.
  if (hasUpcomingVisit(customerId)) return undefined;
  return { last_visit_on: last, visit_count: visits.length, usual_interval_days: usual, allowed_days: allowed, due_on: due, days_overdue: overdue };
}

// ---- Segments ---------------------------------------------------------------------------------------

export interface Criteria {
  species: string[];
  inactive_days: number | null;
  visited_within_days: number | null;
  min_spend_satang: number | null;
  tiers: string[];
  has_active_package: boolean | null;
  branch_id: string | null;
}

export function normaliseCriteria(input: Record<string, any> = {}): Criteria {
  return {
    species: Array.isArray(input.species) ? input.species.map(String).filter(Boolean) : [],
    inactive_days: input.inactive_days ?? null,
    visited_within_days: input.visited_within_days ?? null,
    min_spend_satang: input.min_spend_satang ?? null,
    tiers: Array.isArray(input.tiers) ? input.tiers : [],
    has_active_package: typeof input.has_active_package === 'boolean' ? input.has_active_package : null,
    branch_id: input.branch_id ?? null,
  };
}

function speciesMatches(petSpecies: string, wanted: string): boolean {
  const a = petSpecies.trim().toLowerCase();
  const b = wanted.trim().toLowerCase();
  if (a === b) return true;
  if (isDog(a) && isDog(b)) return true;
  if (isCat(a) && isCat(b)) return true;
  return false;
}

export function matchesCriteria(customerId: string, c: Criteria): boolean {
  if (c.species.length) {
    const pets = petsOf(customerId);
    if (!pets.some((p) => c.species.some((s) => speciesMatches(String(p.species ?? ''), s)))) return false;
  }
  if (c.inactive_days !== null || c.visited_within_days !== null) {
    const last = lastVisitOf(customerId);
    const since = last ? daysBetween(last, day(0)) : Infinity;
    if (c.inactive_days !== null && since < c.inactive_days) return false;
    if (c.visited_within_days !== null && since > c.visited_within_days) return false;
  }
  if (c.min_spend_satang !== null && spendOf(customerId) + standInSpend(customerId) < c.min_spend_satang) return false;
  if (c.tiers.length && !c.tiers.includes(tierOfCustomer(customerId))) return false;
  if (c.has_active_package !== null) {
    const has = packagesOfCustomer(customerId).some((p) => p.status === 'active');
    if (has !== c.has_active_package) return false;
  }
  if (c.branch_id && homeBranchOf(customerId) !== c.branch_id) return false;
  return true;
}

/** Spending the bills of other modules do not show yet, scaled to the stand-in visits. */
function standInSpend(customerId: string): number {
  if (visitDaysOf(customerId).length >= 2) return 0;
  const visits = visitsOf(customerId).length;
  return visits * (60_000 + (hash(customerId) % 140_000));
}

/** Who a broadcast would reach: agreed to marketing and has a LINE address that works. */
export function isReachable(customerId: string): boolean {
  return hasMarketingConsent(customerId) && canReceiveLine(customerId);
}

export function membersOf(c: Criteria): Row[] {
  return activeCustomers().filter((row) => matchesCriteria(row.id, c));
}

export function countSegment(c: Criteria) {
  const members = membersOf(c);
  return { customer_count: members.length, reachable_count: members.filter((m) => isReachable(m.id)).length };
}

/** A customer's display name, or the id when the register has no name for them. */
export function nameOf(customerId: string): string {
  return customer(customerId)?.display_name ?? customerId;
}

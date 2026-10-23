import type { Customer, CustomerLine } from '@/features/customers/types/customer.types';
import type { MyPet } from '@/features/portal/types/portal.types';
import type { ConsentPurpose, ConsentState, ConsentSummary } from '@/features/portal/types/privacy.types';
import type {
  PetAllergy,
  PetCarer,
  PetSummary,
  PetTimelineEntry,
  PetVaccinations,
  VaccinationDose,
  VaccinationDue,
  WeightSeries,
} from '@/features/pets/types/pet.types';
import type { User } from '../../core/auth';
import { day, nextNumber, nowIso, table, type Row } from '../../core/db';

/**
 * The customers module's tables and the helpers other modules use to read them. Rows are kept in
 * the shape the module's main reads answer with (README rule 3), plus a few internal fields that
 * are stripped on the way out by `customerDto()` and `petSummary()`.
 *
 * Tables written here, and only here:
 *   customers, pets, pet_carers, pet_allergies, pet_weights, pet_vaccinations, pet_documents,
 *   portal_invitations, treatment_plans, privacy_policy_versions, consents, data_requests,
 *   referral_codes, referrals
 */

// ---- Row shapes ---------------------------------------------------------------------------------

export interface CustomerRow extends Row, Customer {
  /** The portal account (`users.id`) that signs in as this customer. */
  user_id?: string;
  line?: { line_user_id: string; display_name?: string; picture_url?: string; linked_at: string; unfollowed_at?: string };
  merged_at?: string;
  deleted_at?: string;
}

export interface PetRow extends Row, PetSummary {
  /** A picture for the portal and the counter; a data URL in the demo. */
  photo_url?: string;
  created_at: string;
  /** Appointments the death cancelled, answered again by `deceased/undo`. */
  deceased_cancelled_ids?: string[];
}

export interface CarerRow extends Row, PetCarer {
  pet_id: string;
  created_at: string;
}

export interface AllergyRow extends Row, PetAllergy {
  pet_id: string;
}

export interface WeightRow extends Row {
  pet_id: string;
  recorded_at: string;
  weight_kg: number;
  consultation_id: string;
}

export interface VaccinationRow extends Row, VaccinationDose {
  pet_id: string;
}

export interface PetDocumentRow extends Row {
  pet_id: string;
  kind: string;
  title: string;
  file_name: string;
  content_type: string;
  size_bytes: number;
  visible_to_owner: boolean;
  /** `demo_files` id; the bytes arrive there with the signed `PUT`. */
  file_id: string;
  uploaded_by_staff_id?: string;
  /** Sent by the owner from the portal; the clinic's records then read it as the owner's. */
  uploaded_by_customer_id?: string;
  created_at: string;
}

export interface InvitationRow extends Row {
  customer_id: string;
  sent_to: string;
  invited_by_staff_id: string;
  sent_at: string;
  expires_at: string;
  accepted_at?: string;
}

/** One decision; the current state of a purpose is its latest row. */
export interface ConsentRow extends Row {
  customer_id: string;
  purpose: ConsentPurpose;
  granted: boolean;
  recorded_at: string;
  source: 'portal' | 'counter';
  recorded_by_user_id?: string;
  note?: string;
  policy_version?: string;
}

export interface DataRequestRow extends Row {
  version: number;
  customer_id: string;
  type: 'access' | 'correction' | 'erasure' | 'portability';
  status: 'open' | 'completed' | 'refused';
  raised_at: string;
  detail?: string;
  closed_at?: string;
  closed_by_user_id?: string;
  resolution?: string;
}

export interface ReferralCodeRow extends Row {
  customer_id: string;
  code: string;
  created_at: string;
}

export const customers = () => table<CustomerRow>('customers');
export const pets = () => table<PetRow>('pets');
export const carers = () => table<CarerRow>('pet_carers');
export const allergies = () => table<AllergyRow>('pet_allergies');
export const weights = () => table<WeightRow>('pet_weights');
export const vaccinations = () => table<VaccinationRow>('pet_vaccinations');
export const petDocuments = () => table<PetDocumentRow>('pet_documents');
export const invitations = () => table<InvitationRow>('portal_invitations');
export const consents = () => table<ConsentRow>('consents');
export const dataRequests = () => table<DataRequestRow>('data_requests');
export const referralCodes = () => table<ReferralCodeRow>('referral_codes');

// ---- Answers ------------------------------------------------------------------------------------

/** Leaves out keys whose value is `null` or `undefined`, as the backend's serializer does. */
export function compact<T extends object>(value: T): T {
  return Object.fromEntries(Object.entries(value).filter(([, field]) => field !== undefined && field !== null)) as T;
}

export function customerDto(row: CustomerRow): Customer {
  return compact({
    id: row.id,
    version: row.version,
    display_name: row.display_name,
    phone: row.phone,
    email: row.email || undefined,
    has_portal_account: row.has_portal_account,
    is_active: row.is_active,
    merged_into_customer_id: row.merged_into_customer_id,
    created_at: row.created_at,
  });
}

/** `PetSummary` of a pet row (the `GET /staff/pets` item). */
export function petSummary(pet: PetRow): PetSummary {
  return compact({
    id: pet.id,
    version: pet.version,
    hospital_number: pet.hospital_number,
    name: pet.name,
    species: pet.species,
    breed: pet.breed,
    microchip_number: pet.microchip_number,
    primary_customer_id: pet.primary_customer_id,
    is_deceased: pet.is_deceased,
    deceased_at: pet.deceased_at,
    nickname: pet.nickname,
    diet: pet.diet,
    behaviour: pet.behaviour,
    sex: pet.sex,
    birth_date: pet.birth_date,
    birth_date_estimated: pet.birth_date ? Boolean(pet.birth_date_estimated) : undefined,
    neutered: pet.neutered,
  });
}

export function customerLine(row: CustomerRow): CustomerLine {
  const line = row.line;
  return compact({
    customer_id: row.id,
    version: row.version,
    linked: Boolean(line),
    can_receive: Boolean(line && !line.unfollowed_at),
    unfollowed_at: line?.unfollowed_at,
    display_name: line?.display_name,
    picture_url: line?.picture_url,
  });
}

// ---- Search --------------------------------------------------------------------------------------

/**
 * Text as the counter's search boxes compare it: one Unicode form (a Thai keyboard may send
 * "ำ" as two code points, NFKC makes both the same), lower case, without spaces, hyphens or
 * zero-width marks, and with "เเ" (two sara e, a common typing habit) read as "แ".
 */
export function searchKey(value: unknown): string {
  return String(value ?? '')
    .normalize('NFKC')
    .toLowerCase()
    .replace(/[\s\p{Cf}-]/gu, '')
    .replace(/เเ/g, 'แ');
}

/** A phone as stored (`0812345678`), from whatever was typed: dashes, spaces, `+66`. */
export function phoneKey(value: unknown): string {
  return String(value ?? '').replace(/\D/g, '').replace(/^66(?=\d{9}$)/, '0');
}

/**
 * Whether a typed query is in any of the fields, compared by `searchKey`; a query of three or more
 * digits is also looked for in the phone numbers by `phoneKey`.
 */
export function matchesSearch(query: string, fields: Array<string | null | undefined>, phones: Array<string | null | undefined> = []): boolean {
  const needle = searchKey(query);
  if (!needle) return true;
  if (fields.some((field) => searchKey(field).includes(needle))) return true;
  const digits = /^[\d\s+()-]+$/.test(query.trim()) ? phoneKey(query) : '';
  return digits.length >= 3 && phones.some((phone) => phoneKey(phone).includes(digits));
}

// ---- Helpers for other modules ------------------------------------------------------------------

/** The customer's name, following a merge to the surviving record; `undefined` for an unknown id. */
export function customerName(id: string | null | undefined): string | undefined {
  let row = customers().get(id);
  if (row?.merged_into_customer_id) row = customers().get(row.merged_into_customer_id) ?? row;
  return row?.display_name;
}

/** The customer record a portal account signs in as. */
export function customerOfUser(userId: string): CustomerRow | undefined {
  return customers().find((row) => row.user_id === userId && !row.merged_into_customer_id && !row.deleted_at);
}

/**
 * The pets a customer is primary owner of, then those they are a live carer for, by name.
 * `includeDeceased` defaults to true (the counter and the portal still show a pet that died).
 */
export function petsOf(customerId: string, options: { includeCarer?: boolean; includeDeceased?: boolean } = {}): PetRow[] {
  const { includeCarer = true, includeDeceased = true } = options;
  const caredFor = includeCarer
    ? new Set(carers().where((link) => link.customer_id === customerId && !link.revoked_at).map((link) => link.pet_id))
    : new Set<string>();
  return pets()
    .where((pet) => (pet.primary_customer_id === customerId || caredFor.has(pet.id)) && (includeDeceased || !pet.is_deceased))
    .sort((a, b) => Number(b.primary_customer_id === customerId) - Number(a.primary_customer_id === customerId) || a.name.localeCompare(b.name, 'th'));
}

export function petName(id: string | null | undefined): string | undefined {
  return pets().get(id)?.name;
}

export function liveCarers(petId: string): CarerRow[] {
  return carers().where((link) => link.pet_id === petId && !link.revoked_at);
}

/** Whether a customer may read a pet's history in the portal (primary owner or a carer allowed to). */
export function mayViewHistory(petId: string, customerId: string): boolean {
  const pet = pets().get(petId);
  if (!pet) return false;
  if (pet.primary_customer_id === customerId) return true;
  return liveCarers(petId).some((link) => link.customer_id === customerId && link.may_view_history);
}

export function allergiesOf(petId: string): PetAllergy[] {
  return allergies()
    .where((row) => row.pet_id === petId)
    .sort((a, b) => (b.recorded_at ?? '').localeCompare(a.recorded_at ?? ''))
    .map(({ pet_id: _pet, ...allergy }) => compact(allergy));
}

export function latestWeight(petId: string): { weight_kg: number; recorded_at: string } | undefined {
  const series = weightSeries(petId).points;
  const last = series[series.length - 1];
  return last ? { weight_kg: last.weight_kg, recorded_at: last.recorded_at } : undefined;
}

/**
 * The weight graph: this module's own weighings, plus any a consultation recorded in its vitals
 * (the clinical module's `consultations` table), oldest first.
 */
export function weightSeries(petId: string): WeightSeries {
  const points = weights()
    .where((row) => row.pet_id === petId)
    .map((row) => ({ recorded_at: row.recorded_at, weight_kg: row.weight_kg, consultation_id: row.consultation_id }));
  const known = new Set(points.map((point) => point.consultation_id));
  for (const consultation of table('consultations').where((row) => row.pet_id === petId && !known.has(row.id))) {
    for (const vitals of (consultation.vitals ?? []) as Array<{ recorded_at: string; weight_kg?: number }>) {
      if (typeof vitals.weight_kg === 'number') {
        points.push({ recorded_at: vitals.recorded_at, weight_kg: vitals.weight_kg, consultation_id: consultation.id });
      }
    }
  }
  points.sort((a, b) => a.recorded_at.localeCompare(b.recorded_at));
  return { pet_id: petId, unit: 'kg', points };
}

/** Records a weighing, e.g. from a consultation's vitals. */
export function recordPetWeight(petId: string, weightKg: number, recordedAt = nowIso(), consultationId = ''): WeightRow {
  return weights().insert({ pet_id: petId, weight_kg: weightKg, recorded_at: recordedAt, consultation_id: consultationId });
}

const DUE_SOON_DAYS = 30;

export function vaccinationsOf(petId: string): PetVaccinations {
  const doses = vaccinations()
    .where((row) => row.pet_id === petId)
    .sort((a, b) => b.administered_at.localeCompare(a.administered_at));
  const today = day(0);
  const soon = day(DUE_SOON_DAYS);
  const due: VaccinationDue[] = [];
  const seen = new Set<string>();
  for (const dose of doses) {
    if (seen.has(dose.vaccine_name)) continue;
    seen.add(dose.vaccine_name);
    if (!dose.next_due_on) continue;
    due.push({
      vaccine_name: dose.vaccine_name,
      next_due_on: dose.next_due_on,
      status: dose.next_due_on < today ? 'overdue' : dose.next_due_on <= soon ? 'due_soon' : 'upcoming',
      last_dose_id: dose.id,
      last_given_at: dose.administered_at,
    });
  }
  due.sort((a, b) => a.next_due_on.localeCompare(b.next_due_on));
  return { pet_id: petId, doses: doses.map(({ pet_id: _pet, ...dose }) => compact(dose)), due };
}

/** Records a dose given (e.g. during a consultation). `nextDueOn` absent means a year on. */
export function recordVaccinationDose(petId: string, dose: Omit<VaccinationDose, 'id'>): VaccinationRow {
  return vaccinations().insert({ ...dose, pet_id: petId });
}

/** A pet as the portal's `MyPet` reads it, for the customer who is looking. */
export function myPet(pet: PetRow, customerId: string): MyPet {
  return {
    id: pet.id,
    version: pet.version,
    name: pet.name,
    nickname: pet.nickname ?? null,
    species: pet.species,
    breed: pet.breed ?? null,
    hospital_number: pet.hospital_number,
    microchip_number: pet.microchip_number ?? null,
    photo_url: pet.photo_url ?? null,
    diet: pet.diet ?? null,
    behaviour: pet.behaviour ?? null,
    is_deceased: pet.is_deceased,
    latest_weight: latestWeight(pet.id) ?? null,
    allergies: allergiesOf(pet.id).map(({ substance, severity }) => ({ substance, severity })),
    may_view_history: mayViewHistory(pet.id, customerId),
    sex: pet.sex,
    birth_date: pet.birth_date ?? null,
    birth_date_estimated: Boolean(pet.birth_date_estimated),
    neutered: pet.neutered ?? null,
  } as MyPet;
}

/** The documents of a pet, newest first; `ownerOnly` keeps those the owner may see. */
export function documentsOf(petId: string, options: { ownerOnly?: boolean } = {}): PetDocumentRow[] {
  return petDocuments()
    .where((row) => row.pet_id === petId && (!options.ownerOnly || row.visible_to_owner))
    .sort((a, b) => b.created_at.localeCompare(a.created_at));
}

/** What a pet's owner may change from the portal: the care notes and the photo. */
export type OwnerProfileChanges = Partial<Pick<PetRow, 'nickname' | 'diet' | 'behaviour' | 'photo_url'>>;

/**
 * The owner's edit of their pet (`PATCH /portal/pets/{id}`), already validated by the caller. Only
 * the owner fields are taken; `undefined` clears one. Answers the stored row, version bumped.
 */
export function updateOwnerProfile(petId: string, changes: OwnerProfileChanges): PetRow | undefined {
  const allowed: OwnerProfileChanges = {};
  for (const field of ['nickname', 'diet', 'behaviour', 'photo_url'] as const) {
    if (field in changes) allowed[field] = changes[field] as never;
  }
  return pets().update(petId, allowed);
}

/**
 * Records a document about to be uploaded (the bytes follow to its signed URL), from the counter
 * (`uploaded_by_staff_id`) or the portal (`uploaded_by_customer_id`). Answers the stored row.
 */
export function addPetDocument(row: Omit<PetDocumentRow, 'id' | 'created_at'> & { created_at?: string }): PetDocumentRow {
  return petDocuments().insert({ ...row, created_at: row.created_at ?? nowIso() });
}

/** `HN-2026-0042`: a running number per year. */
export function nextHospitalNumber(year = new Date().getFullYear()): string {
  return `HN-${year}-${String(nextNumber(`hn-${year}`)).padStart(4, '0')}`;
}

const SPECIES_NAMES: Record<string, string> = { dog: 'สุนัข', cat: 'แมว', rabbit: 'กระต่าย', bird: 'นก' };

/**
 * A new patient for a customer, as `POST /staff/pets` registers one: the next hospital number, and
 * the owner as the pet's primary carer. For booking's `new_pet` too, whose form sends `dog`/`cat`
 * where the counter writes the Thai name. Fields are taken as given; the caller validates.
 */
export function registerPet(
  customerId: string,
  input: { name: string; species: string; breed?: string; microchip_number?: string; sex?: PetRow['sex']; birth_date?: string; birth_date_estimated?: boolean; neutered?: boolean },
): PetRow {
  const now = nowIso();
  const species = input.species.trim();
  const pet = pets().insert({
    version: 1,
    hospital_number: nextHospitalNumber(),
    name: input.name.trim(),
    species: SPECIES_NAMES[species.toLowerCase()] ?? species,
    ...(input.breed?.trim() ? { breed: input.breed.trim() } : {}),
    ...(input.microchip_number ? { microchip_number: input.microchip_number } : {}),
    primary_customer_id: customerId,
    is_deceased: false,
    ...(input.sex ? { sex: input.sex } : {}),
    ...(input.birth_date ? { birth_date: input.birth_date, birth_date_estimated: Boolean(input.birth_date_estimated) } : {}),
    ...(typeof input.neutered === 'boolean' ? { neutered: input.neutered } : {}),
    created_at: now,
  } as Omit<PetRow, 'id'>);
  carers().insert({ pet_id: pet.id, customer_id: customerId, is_primary: true, may_view_history: true, created_at: now });
  return pet;
}

/**
 * A customer record for a new portal account: a counter-registered customer with the same phone
 * gets the account, anyone else a new record. Same behaviour as `auth`'s `linkOrCreateCustomer`.
 */
export function createCustomerForUser(user: User): CustomerRow {
  const digits = (user.phone ?? '').replace(/\D/g, '');
  const existing = customers().find(
    (row) => !row.user_id && !row.merged_into_customer_id && row.is_active && row.phone.replace(/\D/g, '') === digits,
  );
  if (existing) return customers().update(existing.id, { user_id: user.id, has_portal_account: true })!;
  return customers().insert({
    version: 1,
    display_name: user.display_name,
    phone: digits,
    ...(user.email ? { email: user.email } : {}),
    has_portal_account: true,
    is_active: true,
    created_at: nowIso(),
    user_id: user.id,
  } as CustomerRow);
}

// ---- Extension points ---------------------------------------------------------------------------

/**
 * What another module moves when two customer records are merged. A listener moves its own rows
 * from `duplicateId` to `survivingId` and answers how many it moved per table, for the answer's
 * `moved` counts. Register from the module's `handlers.ts`.
 */
export type MergeListener = (survivingId: string, duplicateId: string) => Record<string, number>;
const mergeListeners: MergeListener[] = [];
export function onCustomerMerge(listener: MergeListener): void {
  mergeListeners.push(listener);
}
export function runMergeListeners(survivingId: string, duplicateId: string): Record<string, number> {
  const moved: Record<string, number> = {};
  for (const listener of mergeListeners) {
    for (const [key, count] of Object.entries(listener(survivingId, duplicateId))) moved[key] = (moved[key] ?? 0) + count;
  }
  return moved;
}

/**
 * Entries for a pet's timeline (`GET /staff/pets/{id}/history`) from another module. When none is
 * registered for a kind, the module reads the clinical tables itself (see pets-history.ts).
 */
export type TimelineSource = { kinds: string[]; entries: (petId: string) => PetTimelineEntry[] };
export const timelineSources: TimelineSource[] = [];
export function addTimelineSource(source: TimelineSource): void {
  timelineSources.push(source);
}

// ---- Consents -----------------------------------------------------------------------------------

export const CONSENT_PURPOSES: ConsentPurpose[] = ['service_messages', 'marketing', 'medical_record_sharing', 'extended_data_processing'];

/** Only `service_messages` is a yes until the customer says otherwise. */
export const defaultConsent = (purpose: ConsentPurpose) => purpose === 'service_messages';

/** A customer's consents with defaults filled in, and the history of decisions newest first. */
export function consentsOf(customerId: string): ConsentSummary & { history: unknown[] } {
  const rows = consents()
    .where((row) => row.customer_id === customerId)
    .sort((a, b) => b.recorded_at.localeCompare(a.recorded_at));
  const states: ConsentState[] = CONSENT_PURPOSES.map((purpose) => {
    const latest = rows.find((row) => row.purpose === purpose);
    return latest
      ? ({ purpose, granted: latest.granted, is_default: false, recorded_at: latest.recorded_at, source: latest.source, policy_version: latest.policy_version ?? null } as ConsentState)
      : ({ purpose, granted: defaultConsent(purpose), is_default: true, recorded_at: null, source: null, policy_version: null } as ConsentState);
  });
  return {
    customer_id: customerId,
    consents: states,
    history: rows.map((row) => ({
      id: row.id,
      purpose: row.purpose,
      granted: row.granted,
      recorded_at: row.recorded_at,
      source: row.source,
      recorded_by_user_id: row.recorded_by_user_id ?? null,
      note: row.note ?? null,
    })),
  };
}

/**
 * Records one consent decision, stamped with the privacy notice in force. Saving the answer already
 * on file is not a new decision, so nothing is written then. Answers whether a row was written.
 * For the privacy endpoints and for other modules (booking's marketing tick box).
 */
export function recordConsent(
  customerId: string,
  purpose: ConsentPurpose,
  granted: boolean,
  options: { source: 'portal' | 'counter'; userId?: string; note?: string; at?: string },
): boolean {
  const state = consentsOf(customerId).consents.find((entry) => entry.purpose === purpose);
  if (state && !state.is_default && state.granted === granted) return false;
  // The notice in force, read here rather than from privacy.ts, which imports this file.
  const label = table('privacy_policy_versions').find((row) => row.status === 'published' && row.is_current)?.label as string | undefined;
  consents().insert({
    customer_id: customerId,
    purpose,
    granted,
    recorded_at: options.at ?? nowIso(),
    source: options.source,
    ...(options.userId ? { recorded_by_user_id: options.userId } : {}),
    ...(options.note ? { note: options.note } : {}),
    ...(label ? { policy_version: label } : {}),
  });
  return true;
}

/** Whether the customer currently agrees to a purpose (defaults applied). For messaging and CRM. */
export function hasConsent(customerId: string, purpose: ConsentPurpose): boolean {
  return consentsOf(customerId).consents.find((state) => state.purpose === purpose)?.granted ?? defaultConsent(purpose);
}

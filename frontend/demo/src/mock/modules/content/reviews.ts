import { DEMO_ACCOUNTS } from '../../core/demo-accounts';
import { at, day, nowIso, table, today, uuid, type Row } from '../../core/db';
import { checkVersion, created, fail, invalid, notFound, page, requireCustomer, requireStaff, route, stateConflict, type Ctx } from '../../core/http';
import { SAMPLE_INVITATIONS, SAMPLE_REVIEWS } from './reviews-data';

/**
 * Customer reviews (docs/contract-extensions.md § 36): the public list and its summary, the
 * one-time invitation link, the portal's own reviews, and staff moderation. A row is the staff
 * shape (`StaffReview`) plus the service name the public card shows.
 *
 * The seed runs before the customers and appointments seeds, so a seeded review starts with a
 * placeholder customer and visit. The first read links each one to a real customer who owns a pet
 * of the same species (and to one of that pet's visits when there is one), and the first name the
 * public page shows becomes that customer's, as the product would have kept it.
 */

type RejectReason = 'personal_data' | 'offensive' | 'off_topic' | 'spam' | 'customer_request';

export interface ReviewRow extends Row {
  status: 'pending' | 'published' | 'rejected';
  rating: number;
  text?: string | null;
  display_name: string;
  name_choice: 'first_name' | 'anonymous';
  pet_species?: string | null;
  service_name?: string | null;
  source: 'portal' | 'invitation';
  created_at: string;
  visit: {
    visit_id: string;
    visit_on: string;
    branch_id: string;
    branch_name?: string | null;
    pet_id?: string | null;
    pet_name?: string | null;
  };
  customer: { id: string; name?: string | null };
  moderation: {
    moderated_at?: string | null;
    moderated_by_staff_id?: string | null;
    published_at?: string | null;
    reject_reason?: RejectReason | null;
    reject_reason_label?: string | null;
  };
  reply?: { text: string; replied_at: string; replied_by_staff_id?: string | null } | null;
  version: number;
  /** False until the first read has tied the seeded review to a real customer. */
  linked?: boolean;
  invitation_id?: string;
}

export interface InvitationRow extends Row {
  token: string;
  customer_id?: string | null;
  branch_id?: string | null;
  visit_id: string;
  visit_on: string;
  service_name?: string | null;
  pet_name?: string | null;
  pet_species?: string | null;
  created_at: string;
  expires_at: string;
  used_at?: string | null;
  review_id?: string | null;
}

export const reviews = () => table<ReviewRow>('reviews');
export const reviewInvitations = () => table<InvitationRow>('review_invitations');

const SCOPE = 'content:manage';
const WINDOW_DAYS = 60;
const ANONYMOUS = 'ลูกค้า';

const REJECT_LABELS: Record<RejectReason, string> = {
  personal_data: 'มีข้อมูลส่วนบุคคล',
  offensive: 'ภาษาไม่สุภาพ',
  off_topic: 'ไม่เกี่ยวกับบริการของคลินิก',
  spam: 'สแปมหรือซ้ำ',
  customer_request: 'ลูกค้าขอให้นำออก',
};

/** The local calendar day of an instant, `YYYY-MM-DD`. */
function dateOf(iso: string): string {
  const date = new Date(iso);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

// ---- Names (the backend's ReviewNames) -----------------------------------------------------------

const THAI_TITLES = ['นางสาว', 'ด.ช.', 'ด.ญ.', 'น.ส.', 'นาง', 'นาย', 'คุณ', 'ดร.'];

export function firstNameOf(name: string | null | undefined): string | null {
  let text = (name ?? '').replace(/\s+/g, ' ').trim();
  if (!text) return null;
  for (const title of THAI_TITLES) {
    if (!text.startsWith(title)) continue;
    const rest = text.slice(title.length);
    if (!rest.trim()) return null;
    if (rest.startsWith(' ') || /^[ก-ฮเ-ไ]/.test(rest)) {
      text = rest.trim();
      break;
    }
  }
  text = text.replace(/^(mrs?\.?|miss|ms\.?|dr\.?)\s+/i, '');
  const first = text.split(' ')[0];
  return first ? first.slice(0, 40) : null;
}

// ---- Seed ----------------------------------------------------------------------------------------

export function seedReviews(): void {
  const branch = table('branches').all()[0];
  const owner = table('staff_members').find((member) => member.user_id === DEMO_ACCOUNTS.owner.user_id);

  const rows = SAMPLE_REVIEWS.map((sample, index) => {
    const created = at(-sample.days_ago, `${String(9 + (index % 10)).padStart(2, '0')}:${index % 2 ? '45' : '15'}`);
    const moderated = sample.status === 'pending' ? null : at(-sample.days_ago, '18:30');
    const replyAt = sample.reply ? at(-sample.days_ago + (sample.reply_days_after ?? 1), '10:20') : null;
    return {
      status: sample.status,
      rating: sample.rating,
      text: sample.text ?? null,
      display_name: sample.anonymous ? ANONYMOUS : sample.name,
      name_choice: sample.anonymous ? 'anonymous' : 'first_name',
      pet_species: sample.species,
      service_name: sample.service,
      source: sample.source ?? (index % 3 === 0 ? 'invitation' : 'portal'),
      created_at: created,
      visit: {
        visit_id: uuid(),
        visit_on: day(-sample.days_ago - 1),
        branch_id: branch?.id ?? '',
        branch_name: branch?.name ?? null,
        pet_id: null,
        pet_name: null,
      },
      customer: { id: uuid(), name: sample.anonymous ? null : sample.name },
      moderation: {
        moderated_at: moderated,
        moderated_by_staff_id: moderated ? (owner?.id ?? null) : null,
        published_at: sample.status === 'published' ? moderated : null,
        reject_reason: sample.reject_reason ?? null,
        reject_reason_label: sample.reject_reason ? REJECT_LABELS[sample.reject_reason] : null,
      },
      reply: sample.reply && replyAt ? { text: sample.reply, replied_at: replyAt, replied_by_staff_id: owner?.id ?? null } : null,
      version: 1,
      linked: false,
    } satisfies Omit<ReviewRow, 'id'>;
  });
  const stored = reviews().insertMany(rows);

  for (const sample of SAMPLE_INVITATIONS) {
    const created = at(-sample.days_ago, '19:00');
    const used = sample.state === 'used' ? stored.find((row) => row.status === 'published' && row.source === 'invitation') : undefined;
    const invitation = reviewInvitations().insert({
      token: sample.token,
      customer_id: null,
      branch_id: branch?.id ?? null,
      visit_id: used?.visit.visit_id ?? uuid(),
      visit_on: day(-sample.days_ago),
      service_name: sample.service,
      pet_name: sample.pet_name,
      pet_species: null,
      created_at: created,
      expires_at: new Date(new Date(created).getTime() + 30 * 86_400_000).toISOString(),
      used_at: used ? used.created_at : null,
      review_id: used?.id ?? null,
    });
    if (used) reviews().update(used.id, { invitation_id: invitation.id, version: used.version });
  }
}

// ---- Linking seeded reviews to the customers seed ------------------------------------------------

function isDemoCustomer(customer: Row): boolean {
  return customer.user_id === DEMO_ACCOUNTS.customer.user_id;
}

/** Ties seeded reviews and invitations to real customers, pets and visits, once. */
export function linkReviews(): void {
  const pending = reviews().where((row) => row.linked === false);
  const openInvitations = reviewInvitations().where((row) => !row.customer_id);
  if (!pending.length && !openInvitations.length) return;

  const customers = table('customers').where((row) => row.is_active !== false && !row.merged_into_customer_id && !isDemoCustomer(row));
  if (!customers.length) return;
  const pets = table('pets').where((pet) => !pet.is_deceased);
  const visits = table('appointments').where((appointment) => appointment.status === 'completed');
  const owner = table('staff_members').find((member) => member.user_id === DEMO_ACCOUNTS.owner.user_id);
  const used = new Set<string>();
  const usedNames = new Set<string>();

  for (const review of pending) {
    const wanted = review.display_name !== ANONYMOUS ? review.display_name : undefined;
    const candidates = customers.filter((customer) => !used.has(customer.id) && !usedNames.has(firstNameOf(customer.display_name) ?? ''));
    const ownsSpecies = (customer: Row) => pets.some((pet) => pet.primary_customer_id === customer.id && pet.species === review.pet_species);
    const customer =
      (wanted && candidates.find((row) => firstNameOf(row.display_name) === wanted && ownsSpecies(row))) ||
      candidates.find(ownsSpecies) ||
      candidates[0] ||
      customers[Math.floor(Math.random() * customers.length)]!;
    used.add(customer.id);
    usedNames.add(firstNameOf(customer.display_name) ?? '');

    const pet =
      pets.find((row) => row.primary_customer_id === customer.id && row.species === review.pet_species) ??
      pets.find((row) => row.primary_customer_id === customer.id);
    const visit = visits.find((row) => row.customer_id === customer.id && (!pet || row.pet_id === pet.id) && !reviews().find((r) => r.visit.visit_id === row.id));
    const branch = table('branches').get(visit?.branch_id ?? review.visit.branch_id) ?? table('branches').all()[0];

    reviews().update(review.id, {
      linked: true,
      version: review.version,
      display_name: review.name_choice === 'first_name' ? (firstNameOf(customer.display_name) ?? ANONYMOUS) : ANONYMOUS,
      customer: { id: customer.id, name: customer.display_name },
      pet_species: pet?.species ?? review.pet_species,
      visit: {
        visit_id: visit?.id ?? review.visit.visit_id,
        visit_on: visit ? dateOf(visit.starts_at) : review.visit.visit_on,
        branch_id: branch?.id ?? review.visit.branch_id,
        branch_name: branch?.name ?? review.visit.branch_name ?? null,
        pet_id: pet?.id ?? null,
        pet_name: pet?.name ?? null,
      },
      moderation: {
        ...review.moderation,
        moderated_by_staff_id: review.moderation.moderated_at ? (review.moderation.moderated_by_staff_id ?? owner?.id ?? null) : null,
      },
      reply: review.reply ? { ...review.reply, replied_by_staff_id: review.reply.replied_by_staff_id ?? owner?.id ?? null } : null,
    });
  }

  // An invitation goes to a customer without a portal account, the way the product sends them.
  const withoutPortal = customers.filter((row) => !row.has_portal_account);
  openInvitations.forEach((invitation, index) => {
    const review = invitation.review_id ? reviews().get(invitation.review_id) : undefined;
    const customer = review ? { id: review.customer.id } : (withoutPortal[index] ?? customers[index % customers.length]!);
    const pet = pets.find((row) => row.primary_customer_id === customer.id);
    reviewInvitations().update(invitation.id, {
      customer_id: customer.id,
      pet_name: review?.visit.pet_name ?? pet?.name ?? invitation.pet_name,
      pet_species: pet?.species ?? null,
    });
  });
}

// ---- Shapes --------------------------------------------------------------------------------------

function publicReview(row: ReviewRow) {
  return {
    id: row.id,
    rating: row.rating,
    text: row.text ?? null,
    display_name: row.display_name,
    pet_species: row.pet_species ?? null,
    service_name: row.service_name ?? null,
    created_at: row.created_at,
    reply: row.reply ? { text: row.reply.text, replied_at: row.reply.replied_at } : null,
  };
}

function staffReview(row: ReviewRow) {
  const { linked: _linked, invitation_id: _invitation, service_name: _service, ...rest } = row;
  return rest;
}

function submitted(row: ReviewRow) {
  return { id: row.id, status: row.status, rating: row.rating, text: row.text ?? null, display_name: row.display_name, created_at: row.created_at };
}

/** Published reviews, newest first. */
export function publishedReviews(): ReviewRow[] {
  linkReviews();
  return reviews()
    .where((row) => row.status === 'published')
    .sort((a, b) => b.created_at.localeCompare(a.created_at));
}

export function reviewSummary(rows = publishedReviews()) {
  const by_rating: Record<string, number> = { '1': 0, '2': 0, '3': 0, '4': 0, '5': 0 };
  for (const row of rows) by_rating[String(row.rating)] = (by_rating[String(row.rating)] ?? 0) + 1;
  const average = rows.length ? Math.round((rows.reduce((sum, row) => sum + row.rating, 0) / rows.length) * 10) / 10 : null;
  return { average, count: rows.length, by_rating };
}

// ---- Writing a review (ReviewRules) --------------------------------------------------------------

function checkedInput(body: any): { rating: number; text: string | null; name_choice: 'first_name' | 'anonymous' } {
  const rating = Number(body?.rating);
  if (!Number.isInteger(rating) || rating < 1 || rating > 5) invalid('rating', 'ต้องให้คะแนน 1 ถึง 5 ดาว');
  const text = typeof body?.text === 'string' ? body.text.replace(/\r\n?/g, '\n').trim() : '';
  if (text.length > 1000) invalid('text', 'รีวิวยาวได้ไม่เกิน 1,000 ตัวอักษร');
  const choice = body?.display_name;
  if (choice !== 'first_name' && choice !== 'anonymous') invalid('display_name', 'display_name ต้องเป็น first_name หรือ anonymous');
  return { rating, text: text || null, name_choice: choice };
}

function alreadyReviewed(): never {
  fail(409, 'review_already_submitted', 'การมาครั้งนี้ได้รับรีวิวแล้ว รีวิวหนึ่งครั้งต่อการมาหนึ่งครั้ง', { message_en: 'This visit has already been reviewed.' });
}

function writeReview(input: ReturnType<typeof checkedInput>, details: {
  source: 'portal' | 'invitation';
  customerId: string;
  visit: ReviewRow['visit'];
  petSpecies?: string | null;
  serviceName?: string | null;
  invitationId?: string;
}): ReviewRow {
  const customer = table('customers').get(details.customerId);
  return reviews().insert({
    status: 'pending',
    rating: input.rating,
    text: input.text,
    display_name: input.name_choice === 'first_name' ? (firstNameOf(customer?.display_name) ?? ANONYMOUS) : ANONYMOUS,
    name_choice: input.name_choice,
    pet_species: details.petSpecies ?? null,
    service_name: details.serviceName ?? null,
    source: details.source,
    created_at: nowIso(),
    visit: details.visit,
    customer: { id: details.customerId, name: customer?.display_name ?? null },
    moderation: { moderated_at: null, moderated_by_staff_id: null, published_at: null, reject_reason: null, reject_reason_label: null },
    reply: null,
    version: 1,
    linked: true,
    invitation_id: details.invitationId,
  });
}

// ---- Public --------------------------------------------------------------------------------------

route('GET', '/reviews', ({ query }) => {
  const rows = publishedReviews();
  const limit = Math.min(100, Math.max(1, Number(query.get('limit')) || 20));
  const listed = page(rows.map(publicReview), new URLSearchParams({ limit: String(limit), cursor: query.get('cursor') ?? '' }));
  return { ...listed, summary: reviewSummary(rows) };
});

function invitationOf(token: string): InvitationRow {
  linkReviews();
  const invitation = reviewInvitations().find((row) => row.token === token);
  if (!invitation) notFound('ลิงก์รีวิว');
  return invitation;
}

function invitationStatus(row: InvitationRow): 'usable' | 'used' | 'expired' {
  if (row.review_id || row.used_at) return 'used';
  return row.expires_at <= nowIso() ? 'expired' : 'usable';
}

route('GET', '/reviews/invitations/:token', ({ params }) => {
  const invitation = invitationOf(params.token ?? "");
  return {
    status: invitationStatus(invitation),
    expires_at: invitation.expires_at,
    visit: { visit_on: invitation.visit_on, service_name: invitation.service_name ?? null, pet_name: invitation.pet_name ?? null },
  };
});

route('POST', '/reviews/invitations/:token', ({ params, body }) => {
  const invitation = invitationOf(params.token ?? "");
  const status = invitationStatus(invitation);
  if (status === 'used') alreadyReviewed();
  if (status === 'expired') fail(409, 'review_invitation_expired', 'ลิงก์รีวิวนี้หมดอายุแล้ว', { message_en: 'This review link has expired.' });
  const input = checkedInput(body);
  const branch = table('branches').get(invitation.branch_id) ?? table('branches').all()[0];
  const row = writeReview(input, {
    source: 'invitation',
    customerId: invitation.customer_id ?? uuid(),
    visit: {
      visit_id: invitation.visit_id,
      visit_on: invitation.visit_on,
      branch_id: branch?.id ?? '',
      branch_name: branch?.name ?? null,
      pet_id: null,
      pet_name: invitation.pet_name ?? null,
    },
    petSpecies: invitation.pet_species,
    serviceName: invitation.service_name,
    invitationId: invitation.id,
  });
  reviewInvitations().update(invitation.id, { used_at: row.created_at, review_id: row.id });
  return created(submitted(row));
});

// ---- Portal --------------------------------------------------------------------------------------

/** The signed-in owner's visits of the last 60 days that have no review yet. */
function pendingVisits(customerId: string) {
  const firstDay = day(-WINDOW_DAYS);
  const reviewed = new Set(reviews().all().map((row) => row.visit.visit_id));
  return table('appointments')
    .where(
      (row) =>
        row.customer_id === customerId &&
        row.status === 'completed' &&
        dateOf(row.starts_at) >= firstDay &&
        dateOf(row.starts_at) <= today() &&
        !reviewed.has(row.id),
    )
    .sort((a, b) => b.starts_at.localeCompare(a.starts_at))
    .map((row) => {
      const pet = table('pets').get(row.pet_id);
      const branch = table('branches').get(row.branch_id);
      const visitOn = dateOf(row.starts_at);
      const until = new Date(`${visitOn}T00:00:00`);
      until.setDate(until.getDate() + WINDOW_DAYS);
      return {
        visit_id: row.id,
        visit_on: visitOn,
        branch_id: row.branch_id,
        branch_name: branch?.name ?? '',
        service_name: table('services').get(row.service_id)?.name ?? null,
        pet_id: row.pet_id,
        pet_name: pet?.name ?? '',
        pet_species: pet?.species ?? '',
        reviewable_until: `${until.getFullYear()}-${String(until.getMonth() + 1).padStart(2, '0')}-${String(until.getDate()).padStart(2, '0')}`,
      };
    });
}

route('GET', '/portal/reviews/pending', (ctx) => {
  const auth = requireCustomer(ctx);
  const customer = table('customers').get(auth.customerId);
  return { first_name: firstNameOf(customer?.display_name), data: pendingVisits(auth.customerId) };
});

route('POST', '/portal/reviews', (ctx) => {
  const auth = requireCustomer(ctx);
  const visitId = String(ctx.body?.visit_id ?? '');
  const appointment = table('appointments').get(visitId);
  if (!appointment || appointment.customer_id !== auth.customerId) notFound('การมาใช้บริการ');
  if (reviews().find((row) => row.visit.visit_id === visitId)) alreadyReviewed();
  const visit = pendingVisits(auth.customerId).find((row) => row.visit_id === visitId);
  if (!visit) invalid('visit_id', `รีวิวได้เฉพาะการมาใช้บริการที่เสร็จแล้วภายใน ${WINDOW_DAYS} วัน`);
  const input = checkedInput(ctx.body);
  const row = writeReview(input, {
    source: 'portal',
    customerId: auth.customerId,
    visit: {
      visit_id: visit.visit_id,
      visit_on: visit.visit_on,
      branch_id: visit.branch_id,
      branch_name: visit.branch_name,
      pet_id: visit.pet_id,
      pet_name: visit.pet_name,
    },
    petSpecies: visit.pet_species,
    serviceName: visit.service_name,
  });
  return created(submitted(row));
});

// ---- Staff moderation ----------------------------------------------------------------------------

route('GET', '/staff/reviews', (ctx) => {
  requireStaff(ctx, SCOPE);
  linkReviews();
  const status = ctx.query.get('status');
  if (status && !['pending', 'published', 'rejected'].includes(status)) invalid('status', 'status ต้องเป็น pending, published หรือ rejected');
  const rows = reviews()
    .where((row) => !status || row.status === status)
    .sort((a, b) => b.created_at.localeCompare(a.created_at));
  return page(rows.map(staffReview), ctx.query);
});

function moderated(ctx: Ctx): { row: ReviewRow; staffId: string } {
  const auth = requireStaff(ctx, SCOPE);
  linkReviews();
  const row = reviews().get(ctx.params.id);
  if (!row) notFound('รีวิว');
  checkVersion(ctx, row);
  return { row, staffId: auth.staffId };
}

route('POST', '/staff/reviews/:id/publish', (ctx) => {
  const { row, staffId } = moderated(ctx);
  if (row.status !== 'pending') stateConflict(row.status === 'published' ? 'รีวิวนี้เผยแพร่อยู่แล้ว' : 'รีวิวที่ไม่เผยแพร่แล้วนำกลับมาเผยแพร่ไม่ได้');
  const now = nowIso();
  return staffReview(
    reviews().update(row.id, {
      status: 'published',
      moderation: { ...row.moderation, moderated_at: now, moderated_by_staff_id: staffId, published_at: now },
    })!,
  );
});

route('POST', '/staff/reviews/:id/reject', (ctx) => {
  const { row, staffId } = moderated(ctx);
  const reason = ctx.body?.reason as RejectReason;
  if (!REJECT_LABELS[reason]) invalid('reason', 'ต้องเลือกเหตุผลจากรายการ');
  if (row.status === 'rejected') stateConflict('รีวิวนี้ไม่เผยแพร่อยู่แล้ว');
  const now = nowIso();
  return staffReview(
    reviews().update(row.id, {
      status: 'rejected',
      moderation: { ...row.moderation, moderated_at: now, moderated_by_staff_id: staffId, reject_reason: reason, reject_reason_label: REJECT_LABELS[reason] },
    })!,
  );
});

route('POST', '/staff/reviews/:id/reply', (ctx) => {
  const { row, staffId } = moderated(ctx);
  if (row.status === 'rejected') stateConflict('ตอบรีวิวที่ไม่เผยแพร่ไม่ได้');
  const text = typeof ctx.body?.text === 'string' ? ctx.body.text.replace(/\r\n?/g, '\n').trim() : '';
  if (!text) invalid('text', 'ต้องมีข้อความตอบกลับ');
  if (text.length > 1000) invalid('text', 'ข้อความตอบกลับยาวได้ไม่เกิน 1,000 ตัวอักษร');
  return staffReview(reviews().update(row.id, { reply: { text, replied_at: nowIso(), replied_by_staff_id: staffId } })!);
});

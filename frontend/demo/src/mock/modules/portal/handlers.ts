import type { Invoice } from '@/features/portal/types/invoice.types';
import type { MyPet, PortalHome, PortalQueueItem, VaccinationDue } from '@/features/portal/types/portal.types';
import { minutesFromNow } from '../../core/db';
import { fileUrl, getFile, signedUpload } from '../../core/files';
import { created, fail, notFound, page, requireCustomer, route, checkVersion, type Ctx } from '../../core/http';
import { aheadOf, appointmentDto, appointmentsOfCustomer, liveTicketsOfCustomer, tickets, type TicketRow } from '../appointments';
import { invoicesOfCustomer } from '../billing/api';
import { ownerRecords } from '../clinical/records';
import { latestNotificationsOf, unreadCountOf } from '../crm/api';
import { addPetDocument, customers, myPet, petDocuments, petsOf, updateOwnerProfile, vaccinationsOf, type OwnerProfileChanges, type PetRow } from '../customers/store';

/**
 * The owner's portal: what `features/portal` reads that no other module answers. Everything is
 * built from the owning modules' helpers and is scoped to the signed-in customer, so another
 * household's pet, ticket or bill is a plain 404, never a 403 (the backend's rule: confirming that
 * an id exists would already say too much).
 *
 * Other `/portal/*` reads live with their owners: appointments (`/portal/appointments`,
 * `/portal/appointment-types`), crm (`/portal/thread*`, `/portal/notifications*`, `/portal/packages`),
 * customers (`/portal/consents`, `/portal/data-requests`, `/portal/referral`), content (`/portal/reviews*`).
 */

/** Appointment states that still hold a time on the calendar (`AppointmentTransitions.OccupyingStatuses`). */
const OCCUPYING = ['booked', 'confirmed', 'checked_in', 'in_consultation'];

/** What an owner may send (`features/portal/lib/document-rules.ts`), and how big. */
const DOCUMENT_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/heic', 'application/pdf'];
const DOCUMENT_KINDS = ['photo', 'lab_result', 'referral', 'imaging', 'other'];
const DOCUMENT_MAX_BYTES = 10 * 1024 * 1024;

const OWNER_TEXT_LIMITS = { nickname: 60, diet: 500, behaviour: 1000 } as const;

// ---- Reading the household ---------------------------------------------------------------------

/** A pet this customer owns or cares for; anything else is "not found", as the backend answers. */
function myPetRow(customerId: string, petId: string): PetRow {
  const pet = petsOf(customerId).find((row) => row.id === petId);
  if (!pet) notFound('สัตว์เลี้ยง');
  return pet;
}

function myPets(customerId: string): MyPet[] {
  return petsOf(customerId).map((pet) => myPet(pet, customerId));
}

function queueItem(ticket: TicketRow): PortalQueueItem {
  return {
    ticket_id: ticket.id,
    number: ticket.number,
    status: ticket.status,
    // `aheadOf` is the ticket's place among those still waiting; a called ticket has no one ahead.
    ahead: Math.max(0, aheadOf(ticket)),
    pet_id: ticket.pet_id,
    appointment_id: ticket.appointment_id ?? null,
  };
}

/** Vaccines overdue or due within 30 days, for the living pets (`GetPortalHomeHandler`). */
function vaccinationsDue(customerId: string): VaccinationDue[] {
  return petsOf(customerId)
    .filter((pet) => !pet.is_deceased)
    .flatMap((pet) =>
      vaccinationsOf(pet.id)
        .due.filter((item) => item.status !== 'upcoming')
        .map((item) => ({
          pet_id: pet.id,
          pet_name: pet.name,
          vaccine_name: item.vaccine_name,
          next_due_on: item.next_due_on,
          status: item.status as VaccinationDue['status'],
        })),
    )
    .sort((a, b) => a.next_due_on.localeCompare(b.next_due_on));
}

/** A bill as the owner reads it: the staff-only fields (who collected, who approved) left out. */
function portalInvoice(row: ReturnType<typeof invoicesOfCustomer>[number]): Invoice {
  return {
    id: row.id,
    number: row.number,
    status: row.status,
    branch_id: row.branch_id,
    pet_id: row.pet_id ?? null,
    issued_at: row.issued_at,
    note: row.note ?? null,
    subtotal_satang: row.subtotal_satang,
    discount_satang: row.discount_satang,
    total_satang: row.total_satang,
    paid_satang: row.paid_satang,
    refunded_satang: row.refunded_satang,
    balance_satang: row.balance_satang,
    lines: row.lines.map((line) => ({
      id: line.id,
      kind: line.kind,
      description: line.description,
      quantity: line.quantity,
      unit_price_satang: line.unit_price_satang,
      line_total_satang: line.line_total_satang,
    })),
    discounts: row.discounts.map((discount) => ({
      id: discount.id,
      amount_satang: discount.amount_satang,
      percent: discount.percent ?? null,
      reason: discount.reason,
      applied_at: discount.applied_at,
    })),
    payments: row.payments.map((payment) => ({
      id: payment.id,
      method: payment.method,
      amount_satang: payment.amount_satang,
      collected_at: payment.collected_at,
    })),
    refunds: row.refunds.map((refund) => ({
      id: refund.id,
      method: refund.method,
      amount_satang: refund.amount_satang,
      reason: refund.reason,
      refunded_at: refund.refunded_at,
    })),
    receipt: row.receipt
      ? {
          id: row.receipt.id,
          number: row.receipt.number,
          is_tax_invoice: row.receipt.is_tax_invoice,
          amount_satang: row.receipt.amount_satang,
          issued_at: row.receipt.issued_at,
        }
      : null,
    voided_at: row.voided_at ?? null,
    void_reason: row.void_reason ?? null,
  };
}

function validationFailed(errors: Record<string, string[]>): never {
  const [field, messages] = Object.entries(errors)[0]!;
  fail(422, 'validation_failed', messages[0]!, { field, details: { errors } });
}

// ---- Home ------------------------------------------------------------------------------------------

/** `GET /portal/home`: pets, the next visit, today's tickets and the inbox in one read. */
route('GET', '/portal/home', (ctx) => {
  const { customerId } = requireCustomer(ctx);
  const customer = customers().get(customerId);
  if (!customer) notFound('ลูกค้า');

  const next = appointmentsOfCustomer(customerId, { upcoming: true }).find((row) => OCCUPYING.includes(row.status));
  const home: PortalHome = {
    customer_id: customer.id,
    display_name: customer.display_name,
    pets: myPets(customerId),
    next_appointment: next ? (appointmentDto(next) as PortalHome['next_appointment']) : null,
    queue: liveTicketsOfCustomer(customerId).map(queueItem),
    unread_notifications: unreadCountOf(customerId),
    latest_notifications: latestNotificationsOf(customerId, 3),
    vaccinations_due: vaccinationsDue(customerId),
  };
  return home;
});

// ---- Pets ------------------------------------------------------------------------------------------

route('GET', '/portal/pets', (ctx) => {
  const { customerId } = requireCustomer(ctx);
  return { data: myPets(customerId) };
});

/**
 * `PATCH /portal/pets/{id}`: the four owner fields only. An empty string clears a field, a missing
 * one is left alone. The photo must be an image already uploaded to this pet's own documents.
 */
route('PATCH', '/portal/pets/:id', (ctx) => {
  const { customerId } = requireCustomer(ctx);
  const pet = myPetRow(customerId, ctx.params.id!);
  checkVersion(ctx, pet);

  const body = ctx.body ?? {};
  const changes: OwnerProfileChanges = {};
  const errors: Record<string, string[]> = {};
  for (const field of ['nickname', 'diet', 'behaviour'] as const) {
    if (body[field] === undefined || body[field] === null) continue;
    const text = String(body[field]).trim();
    if (text.length > OWNER_TEXT_LIMITS[field]) errors[field] = [`ยาวได้ไม่เกิน ${OWNER_TEXT_LIMITS[field]} ตัวอักษร`];
    else changes[field] = (text || undefined) as never;
  }

  const photoId = typeof body.photo_document_id === 'string' ? body.photo_document_id : '';
  if (photoId) {
    const document = petDocuments().get(photoId);
    const stored = document && getFile(document.file_id);
    if (!document || document.pet_id !== pet.id || !document.content_type.startsWith('image/') || !stored || stored.size <= 0) {
      errors.photo_document_id = ['รูปต้องเป็นไฟล์รูปภาพของสัตว์ตัวนี้ที่อัปโหลดเสร็จแล้ว'];
    } else {
      changes.photo_url = fileUrl(document.file_id) ?? undefined;
    }
  }
  if (Object.keys(errors).length) validationFailed(errors);

  const updated = updateOwnerProfile(pet.id, changes);
  return myPet(updated ?? pet, customerId);
});

/** `GET /portal/pets/{id}/records`: only the sections the clinic opens to owners. */
route('GET', '/portal/pets/:id/records', (ctx) => {
  const { customerId } = requireCustomer(ctx);
  const pet = myPetRow(customerId, ctx.params.id!);
  const view = myPet(pet, customerId);
  if (!view.may_view_history) fail(403, 'insufficient_scope', 'เจ้าของหลักยังไม่ได้อนุญาตให้บัญชีนี้ดูประวัติการรักษา');
  return ownerRecords(pet.id);
});

/**
 * `POST /portal/documents`: records the file the owner is about to send and hands out the signed
 * URL the browser `PUT`s the bytes to (served by the demo's `/storage/` in install.ts).
 */
route('POST', '/portal/documents', (ctx) => {
  const { customerId } = requireCustomer(ctx);
  const body = ctx.body ?? {};
  const pet = myPetRow(customerId, String(body.pet_id ?? ''));

  const errors: Record<string, string[]> = {};
  const fileName = String(body.file_name ?? '').trim();
  const contentType = String(body.content_type ?? '').toLowerCase();
  const size = Number(body.size_bytes);
  const kind = body.kind ? String(body.kind) : 'other';
  const title = String(body.title ?? '').trim();
  if (!fileName) errors.file_name = ['ต้องระบุชื่อไฟล์'];
  else if (fileName.length > 200) errors.file_name = ['ชื่อไฟล์ยาวเกินไป'];
  if (!DOCUMENT_TYPES.includes(contentType)) errors.content_type = ['รับเฉพาะไฟล์ JPEG, PNG, WEBP, HEIC หรือ PDF'];
  if (!Number.isFinite(size) || size <= 0) errors.size_bytes = ['ไฟล์ว่างเปล่า'];
  else if (size > DOCUMENT_MAX_BYTES) errors.size_bytes = ['ไฟล์ใหญ่เกิน 10 MB'];
  if (!DOCUMENT_KINDS.includes(kind)) errors.kind = ['ประเภทเอกสารไม่ถูกต้อง'];
  if (title.length > 200) errors.title = ['ชื่อเอกสารยาวได้ไม่เกิน 200 ตัวอักษร'];
  if (Object.keys(errors).length) validationFailed(errors);

  const upload = signedUpload({ name: fileName, content_type: contentType, size: 0 });
  // No `uploaded_by_staff_id`, so the clinic's records read it as the owner's (`source: 'owner'`).
  const document = addPetDocument({
    pet_id: pet.id,
    kind,
    title: title || fileName.replace(/\.[^.]+$/, ''),
    file_name: fileName,
    content_type: contentType,
    size_bytes: size,
    visible_to_owner: true,
    file_id: upload.id,
    uploaded_by_customer_id: customerId,
  });

  return created({
    document: { id: document.id, title: document.title, file_name: fileName },
    upload: { url: upload.url, method: 'PUT', headers: upload.headers, expires_at: minutesFromNow(15) },
  });
});

// ---- Bills -----------------------------------------------------------------------------------------

/** `GET /portal/invoices?unpaid=`: the household's bills, newest first; `unpaid` keeps what is owed. */
route('GET', '/portal/invoices', (ctx) => {
  const { customerId } = requireCustomer(ctx);
  const unpaid = ctx.query.get('unpaid') === 'true';
  const rows = invoicesOfCustomer(customerId).filter((row) => !unpaid || row.status === 'open' || row.status === 'partially_paid');
  return page(rows.map(portalInvoice), ctx.query, 25);
});

// ---- Queue -----------------------------------------------------------------------------------------

/** `GET /portal/queue/{id}`: the owner's own place in line (`MyQueueTicketDto`). */
route('GET', '/portal/queue/:id', (ctx: Ctx) => {
  const { customerId } = requireCustomer(ctx);
  const ticket = tickets().get(ctx.params.id!);
  if (!ticket || ticket.customer_id !== customerId) notFound('คิว');
  return {
    id: ticket.id,
    number: ticket.number,
    status: ticket.status,
    priority: ticket.priority,
    service_date: ticket.service_date,
    branch_id: ticket.branch_id,
    room_id: ticket.room_id ?? null,
    ahead: Math.max(0, aheadOf(ticket)),
    called_at: ticket.called_at ?? null,
  };
});

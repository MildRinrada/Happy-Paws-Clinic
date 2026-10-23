/**
 * What other modules may call on the billing module (bills, stock, files). Everything here writes
 * through this module's own tables, so the cash desk, the stock room, the dashboard and the reports
 * stay in step with whatever another module did.
 *
 * Bills (`invoices` table, rows in the `Invoice` shape of features/billing/types)
 * - `invoicesOfCustomer(customerId)`: the customer's bills, newest first, void ones included.
 * - `outstandingSatangOf(customerId)`: what they still owe over open and partly paid bills.
 * - `createInvoiceForVisit({ customerId, branchId, staffId, lines, petId?, consultationId?,
 *   appointmentId?, note?, issuedAt? })`: raises a bill. A case or appointment that already has an
 *   open bill gets the lines added to it instead. Service lines with a `source_id` may be covered by
 *   one of the customer's CRM packages (they go on at zero).
 * - `addInvoiceLine(invoiceId, { description, quantity, unit_price_satang, kind?, source_id? })`:
 *   adds a charge to a bill still taking money; `409 state_conflict` on a settled or void one.
 * - `invoiceForVisit({ consultationId?, appointmentId? })`: the live bill of a case or visit.
 * - `onInvoiceEvent((event, invoice) => …)`: `created`, `line_added`, `discounted`, `payment`,
 *   `refunded`, `voided`, `receipt`. The bus also hears `billing` with `{ type: 'invoice.<event>' }`.
 * - `raiseConsultationInvoice(consultation, staffId, at?)`: the bill a closing case raises
 *   (treatments, and medicine already dispensed); null when there is nothing to charge.
 *
 * Stock (`inventory_items`, `inventory_lots`, `stock_movements`, `purchase_orders`)
 * - `consumeStock(itemId, qty, reason, { branchId?, staffId?, prescriptionId?, type?, at? })`:
 *   takes stock out first-expiry-first, writing `dispense` (with a prescription) or `issue`
 *   movements. Throws `409 insufficient_stock`. Answers the movements.
 * - `dispenseMedicine({ prescription, branchId, staffId, consultation? })`: a dispense's stock and
 *   charge together, as `POST /staff/prescriptions/{id}/dispense` reports them.
 * - `findItem(idOrName)`, `availableAt(itemId, branchId)`, `drugOptions(branchId, q?, limit?)`,
 *   `itemView(itemRow, branchIds)`, `ensureInventory()` (plants the stock room if still empty; any
 *   seed that needs items before order 40 may call it).
 *
 * Files
 * - `renderPdf(title, subtitle, lines)`: an A4 PDF with Thai text (pages drawn on a canvas), for a
 *   `postForFile` answer: `file(renderPdf(...), 'application/pdf', 'name.pdf')`.
 * - `toCsv(rows)`, `toXlsx(sheetName, rows)`, `dataUrl(bytes, type)`.
 */

export { addInvoiceLine, createInvoice as createInvoiceForVisit, invoiceForVisit, invoicesOfCustomer, onInvoiceEvent, outstandingSatangOf } from './invoices';
export { availableAt, consumeStock, drugOptions, findItem, itemView } from './inventory';
export { ensureInventory } from './catalogue';
export { dispenseMedicine, raiseConsultationInvoice } from './visits';
export { dataUrl, renderPdf, toCsv, toXlsx } from './documents';

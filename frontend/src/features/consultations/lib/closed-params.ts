/** The address parameters a just-closed case carries, so the page can say what the close produced. */
export const CLOSED_PARAMS = {
  closed: 'closed',
  invoiceId: 'invoice',
  invoiceNumber: 'invoice_number',
  followUpAt: 'follow_up',
} as const;

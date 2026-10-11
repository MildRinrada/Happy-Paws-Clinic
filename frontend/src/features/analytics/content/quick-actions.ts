import type { QuickActionKey } from '../lib/quick-actions';

/** The counter's shortcuts beside the page title. */
export const quickActionText: { label: string } & Record<QuickActionKey, string> = {
  label: 'ทางลัด',
  issueTicket: 'ออกบัตรคิว',
  book: 'สร้างนัด',
  register: 'ลงทะเบียนลูกค้าใหม่',
  collect: 'รับชำระ',
};

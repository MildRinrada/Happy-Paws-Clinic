import type { AppointmentStatus } from '@/features/appointments/types/appointment.types';
import type { VisitStage } from '../lib/visit-stage';
import { portalPaths } from './portal-navigation';

/**
 * Everything the visit card says, and where its links go. Edit the wording here.
 *
 * Chat goes to the contact page, which lists the clinic's LINE account, phone and email: the
 * contract has no portal chat endpoint. A message the owner sends to the clinic's LINE account
 * from a linked LINE reaches the staff inbox (`/messages`). Point `href` at a portal chat page
 * instead if one is ever added; nothing else needs to change.
 */
export const visitCard = {
  liveTitle: 'ติดตามสด คิววันนี้',
  upcomingTitle: 'นัดครั้งถัดไป',

  /** The pill at the top right, by the appointment's state. A state not listed shows no pill. */
  statusBadge: {
    booked: 'รอยืนยัน',
    confirmed: 'ยืนยันแล้ว',
    checked_in: 'เช็กอินแล้ว',
    in_consultation: 'กำลังตรวจ',
  } as Partial<Record<AppointmentStatus, string>>,

  walkInTitle: 'คิว walk-in',
  /** Put before the pet's name after the service: "ตรวจสุขภาพประจำปี น้องโมจิ". */
  petPrefix: 'น้อง',
  vetFallback: 'สัตวแพทย์ที่ว่างในวันนั้น',

  queueLabel: 'คิวของคุณ',
  noTicket: 'ยังไม่มี',
  aheadLabel: 'เหลืออีก',
  aheadValue: (count: number) => (count === 0 ? 'คิวถัดไป' : `${count} คิว`),

  steps: ['เช็กอิน', 'รอเรียก', 'ตรวจ', 'ชำระเงิน'],

  /** How long before the appointment the owner is asked to arrive. */
  arriveMinutesEarly: 15,

  /** The yellow note under the progress bar, by stage. `{ticket}` is replaced with the queue number. */
  notice: {
    before: 'ควรมาถึงคลินิกภายในเวลานี้ เผื่อเวลาเช็กอินและชั่งน้ำหนักก่อนตรวจ',
    waiting: 'อยู่ใกล้ ๆ คลินิกไว้ หน้านี้อัปเดตเองเมื่อคิวขยับ',
    called: 'ถึงคิวแล้ว เชิญที่ห้องตรวจได้เลย',
    in_room: 'น้องกำลังตรวจอยู่ ขอให้ราบรื่นนะ',
    done: 'ตรวจเสร็จแล้ว ชำระเงินได้ที่เคาน์เตอร์',
  } satisfies Record<VisitStage, string>,
  noticeLead: {
    waiting: 'รอเรียก',
    in_room: 'กำลังตรวจ',
    done: 'เสร็จแล้ว',
  } as Partial<Record<VisitStage, string>>,

  checkIn: 'เช็กอินล่วงหน้า',
  checkingIn: 'กำลังเช็กอิน',
  reschedule: { label: 'เลื่อนนัด', href: portalPaths.reschedule },
  chat: { label: 'แชทกับคลินิก', href: '/contact' },

  unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',

  empty: {
    title: 'วันนี้ยังไม่มีนัดหรือคิว',
    body: 'จองนัดล่วงหน้าแล้วติดตามคิวของน้องได้จากหน้านี้',
    book: { label: 'จองนัด', href: '/portal/book' },
  },
};

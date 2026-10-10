import type { BadgeTone } from '@/components/ui/badge';
import type { ArticleStatus } from '../types/staff-article.types';

/** `/settings/articles`: every article, drafts included (docs/contract-extensions.md § 26). */
export const staffArticlesText = {
  metaTitle: 'บทความ · ตั้งค่า · Happy Paws Clinic',
  title: 'บทความบนเว็บไซต์',
  subtitle: 'เขียน แก้ เผยแพร่ และนำบทความสุขภาพสัตว์ลงจากหน้าเว็บของคลินิก',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์จัดการบทความ (ต้องมีสิทธิ์ content:manage)',
  create: 'เขียนบทความใหม่',
  heading: 'บทความทั้งหมด',
  hint: 'บทความที่เขียนใหม่สุดอยู่บนสุด ร่างไม่ขึ้นบนหน้าเว็บจนกว่าจะกดเผยแพร่',
  empty: 'ยังไม่มีบทความตามตัวกรองนี้',
  filters: {
    label: 'กรองบทความ',
    search: 'ค้นจากชื่อเรื่องหรือ slug',
    status: 'สถานะ',
    any: 'ทั้งหมด',
    apply: 'กรอง',
  },
  meta: (author: string, minutes: number) => `โดย ${author} | อ่าน ${minutes} นาที`,
  publishedOn: (when: string) => `เผยแพร่ ${when}`,
  createdOn: (when: string) => `เขียนเมื่อ ${when}`,
  noCover: 'ยังไม่มีรูปปก',
  edit: 'แก้ไข',
  view: 'ดูบนเว็บ',
  next: 'หน้าถัดไป',
  first: 'กลับหน้าแรก',
};

export const articleStatusLabels: Record<ArticleStatus, { text: string; tone: BadgeTone }> = {
  draft: { text: 'ร่าง', tone: 'neutral' },
  scheduled: { text: 'ตั้งเวลาไว้', tone: 'amber' },
  published: { text: 'เผยแพร่แล้ว', tone: 'green' },
};

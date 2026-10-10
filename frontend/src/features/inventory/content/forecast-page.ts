import type { BadgeTone } from '@/components/ui/badge';
import type { ForecastConfidence } from '../types/forecast.types';
import type { ForecastSort } from '../lib/forecast-query';

/** "ยาจะหมดเมื่อไร" (docs/contract-extensions.md § 33). */
export const forecastPage = {
  metaTitle: 'คาดการณ์สต๊อก · Happy Paws Clinic',
  title: 'คาดการณ์สต๊อก',
  subtitle: 'คิดจากการจ่ายออกและการจ่ายยาของสาขานี้เอง ไม่นับการโอนและการปรับยอดหลังนับ',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูสต๊อก (inventory:read)',
  empty: 'ยังไม่มีรายการยาในคลัง',
  how: (lead: number, cover: number) =>
    `อัตราใช้คือค่าเฉลี่ยต่อวันที่สูงกว่าระหว่าง 30 วันกับ 90 วันล่าสุด จำนวนที่ควรสั่งพอใช้ช่วงรอของ ${lead} วัน บวกอีก ${cover} วัน หักของที่ใช้ได้และของที่สั่งไว้แล้ว`,

  cover: 'สั่งให้พอใช้อีก',
  coverOption: (days: number) => `${days} วัน`,
  sort: 'เรียงตาม',
  apply: 'แสดง',

  outIn: (days: number) => (days === 0 ? 'หมดแล้วหรือหมดวันนี้' : `หมดในอีก ${days} วัน`),
  outOn: (day: string) => `ประมาณ ${day}`,
  notUsed: 'ยังไม่มีการใช้ใน 90 วัน',
  rate: (short: number, long: number, unit: string) => `ใช้เฉลี่ย ${short} ${unit}/วัน (30 วัน) ${long} ${unit}/วัน (90 วัน)`,
  stock: (available: number, unit: string) => `ใช้ได้ ${available} ${unit}`,
  onOrder: (count: number) => `สั่งไว้แล้ว ${count}`,
  suggest: (count: number, unit: string) => `ควรสั่ง ${count} ${unit}`,
  noOrder: 'ยังไม่ต้องสั่ง',
  urgent: 'สั่งด่วน หมดก่อนของมาถึง',
  history: (days: number) => `มีข้อมูล ${days} วัน`,
};

export const forecastSortLabels: Record<ForecastSort, string> = {
  days: 'หมดก่อน',
  order: 'ควรสั่งมาก',
  name: 'ชื่อ',
};

export const confidenceLabels: Record<ForecastConfidence, { label: string; tone: BadgeTone }> = {
  low: { label: 'ข้อมูลน้อย ไม่ถึง 14 วัน', tone: 'amber' },
  medium: { label: 'ข้อมูลไม่ถึง 90 วัน', tone: 'blue' },
  high: { label: 'ข้อมูลพอ', tone: 'neutral' },
};

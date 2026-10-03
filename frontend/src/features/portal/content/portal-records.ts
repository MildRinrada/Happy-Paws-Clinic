import type { RecordSection } from '../types/records.types';

/** Everything the "ประวัติการรักษา" page says. Edit the wording here. */
export const portalRecords = {
  title: 'ประวัติการรักษา',
  intro: 'บันทึกการตรวจที่หมอลงนามแล้ว เฉพาะส่วนที่คลินิกเปิดให้เจ้าของดู',
  pickerLabel: 'เลือกสัตว์เลี้ยง',

  noPets: 'ยังไม่มีสัตว์เลี้ยงที่ดูประวัติได้ ถ้าคุณเป็นผู้ดูแลร่วม ขอให้เจ้าของหลักหรือคลินิกเปิดสิทธิ์ให้',
  notFound: 'ไม่พบประวัติของสัตว์ตัวนี้',
  noVisits: 'ยังไม่มีบันทึกการตรวจ',
  onlyDates: 'คลินิกยังไม่ได้เปิดรายละเอียดให้ดูในเว็บ ตอนนี้จึงเห็นแค่วันที่มาตรวจ สอบถามรายละเอียดได้ที่คลินิก',

  visitedOn: (date: string) => `มาตรวจ ${date}`,

  sections: {
    vitals: 'น้ำหนักและสัญญาณชีพ',
    examination: 'การตรวจร่างกาย',
    diagnosis: 'ผลวินิจฉัย',
    treatment: 'การรักษา',
    prescription: 'ยาที่ได้รับ',
    lab_result: 'ผลแล็บ',
    attachments: 'ไฟล์แนบ',
  } satisfies Record<RecordSection, string>,

  vitals: {
    weight: (kg: number) => `น้ำหนัก ${kg} กก.`,
    temperature: (c: number) => `อุณหภูมิ ${c} °C`,
    heartRate: (bpm: number) => `ชีพจร ${bpm} ครั้ง/นาที`,
    respiratoryRate: (bpm: number) => `หายใจ ${bpm} ครั้ง/นาที`,
  },
  normal: 'ปกติ',
  abnormal: 'ผิดปกติ',
  quantity: (count: number) => `${count} ครั้ง`,
  prescription: (dose: string, frequency: string, days: number) => `${dose} ${frequency} ${days} วัน`,
  noneInVisit: 'ไม่มี',

  lab: {
    reviewedOn: (date: string) => `หมออ่านผลแล้ว ${date}`,
    // `normal` draws no badge at all.
    flag: { high: 'สูง', low: 'ต่ำ', abnormal: 'ผิดปกติ' } as Record<string, string>,
  },
  attachment: {
    open: 'เปิดไฟล์',
    fromOwner: 'คุณส่งมา',
  },
};

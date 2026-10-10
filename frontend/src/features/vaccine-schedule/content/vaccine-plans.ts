import type { ScheduleRules, Species, SpeciesPlan } from '../types/vaccine-schedule.types';

/**
 * The general vaccine schedule the public helper (`/vaccine-schedule`) computes from.
 *
 * MEDICAL CONTENT. A veterinarian at the clinic must check and approve this file before the page
 * goes live, and again after every change. Fill in `scheduleReview` when they have; until then the
 * page says the schedule is awaiting the clinic vet's review. See docs/features/vaccine-schedule.md.
 *
 * What it is: a conservative, widely accepted outline of the CORE vaccines for dogs and cats in
 * Thailand, plus rabies, which Thai law requires. No doses, no brand names, no products. The vet
 * decides the actual vaccine, the number of doses and the timing for each animal after examining it.
 *
 * Sources it follows:
 * - WSAVA Guidelines for the Vaccination of Dogs and Cats (World Small Animal Veterinary
 *   Association, 2024 edition, J Small Anim Pract 2024): core vaccines for dogs are canine
 *   distemper, canine adenovirus (infectious hepatitis) and canine parvovirus; for cats, feline
 *   panleukopenia, feline herpesvirus-1 and feline calicivirus; rabies is core where it is endemic,
 *   as in Thailand. Puppies and kittens start at 6 to 8 weeks, repeat every 2 to 4 weeks until
 *   16 weeks or older, and get a booster at 6 to 12 months.
 * - พระราชบัญญัติโรคพิษสุนัขบ้า พ.ศ. 2535: the owner of a dog or cat has it vaccinated against
 *   rabies first at 2 to 4 months of age, then again on the date set in the vaccination
 *   certificate (yearly in usual Thai practice).
 *
 * Choices made here, for the vet to confirm or change:
 * - Series at 8, 12 and 16 weeks (a 4-week spacing inside WSAVA's 2 to 4 weeks).
 * - Rabies first at 12 weeks (inside the legal 2 to 4 months), booster at 1 year, then yearly.
 * - First booster at 1 year of age; WSAVA also allows it at 6 months, which the note mentions.
 * - Leptospirosis, kennel cough and feline leukaemia are listed as optional, for the vet to advise.
 */

/** Who approved this schedule and when. Leave both empty until a clinic vet has. */
export const scheduleReview: { reviewedBy?: string; reviewedOn?: string } = {};

export const scheduleRules: ScheduleRules = {
  minGapDays: 21,
  dueSoonDays: 14,
  horizonMonths: 12,
};

const rabies = {
  id: 'rabies',
  name: 'วัคซีนพิษสุนัขบ้า',
  protects: 'โรคพิษสุนัขบ้า ซึ่งติดต่อถึงคนได้และยังพบในไทย กฎหมายกำหนดให้เจ้าของสุนัขและแมวพาไปฉีด',
};

export const vaccinePlans: Record<Species, SpeciesPlan> = {
  dog: {
    species: 'dog',
    label: 'น้องหมา',
    vaccines: [
      {
        id: 'core',
        name: 'วัคซีนรวมหลักของสุนัข',
        protects: 'ไข้หัดสุนัข ลำไส้อักเสบจากเชื้อพาร์โวไวรัส และตับอักเสบติดต่อ',
      },
      rabies,
    ],
    visits: [
      {
        id: 'series-1',
        title: 'วัคซีนรวมเข็มที่ 1',
        when: 'อายุ 8 สัปดาห์',
        timing: { kind: 'age-weeks', weeks: 8 },
        vaccineIds: ['core'],
      },
      {
        id: 'series-2',
        title: 'วัคซีนรวมเข็มที่ 2 และพิษสุนัขบ้าเข็มแรก',
        when: 'อายุ 12 สัปดาห์',
        timing: { kind: 'age-weeks', weeks: 12 },
        vaccineIds: ['core', 'rabies'],
        note: 'กฎหมายกำหนดให้ฉีดพิษสุนัขบ้าครั้งแรกเมื่ออายุ 2 ถึง 4 เดือน',
      },
      {
        id: 'series-3',
        title: 'วัคซีนรวมเข็มที่ 3',
        when: 'อายุ 16 สัปดาห์',
        timing: { kind: 'age-weeks', weeks: 16 },
        vaccineIds: ['core'],
        note: 'เข็มสุดท้ายของชุดลูกสุนัข ควรฉีดเมื่ออายุ 16 สัปดาห์ขึ้นไป',
      },
      {
        id: 'first-booster',
        title: 'กระตุ้นครบ 1 ปี',
        when: 'อายุ 1 ปี',
        timing: { kind: 'age-months', months: 12 },
        vaccineIds: ['core', 'rabies'],
        note: 'สัตวแพทย์บางท่านนัดกระตุ้นเร็วขึ้นเมื่ออายุราว 6 เดือน',
      },
    ],
    repeat: {
      id: 'yearly',
      title: 'กระตุ้นประจำปี',
      when: 'ทุกปีหลังจากนั้น',
      timing: { kind: 'after-previous-months', months: 12 },
      vaccineIds: ['rabies', 'core'],
      note: 'พิษสุนัขบ้าฉีดทุกปีตามใบรับรองการฉีด ส่วนวัคซีนรวม สัตวแพทย์อาจให้เว้นเป็นทุก 3 ปีตามชนิดวัคซีน',
    },
    optional: [
      {
        name: 'เลปโตสไปโรซิส (โรคฉี่หนู)',
        detail: 'ในไทยมักฉีดคู่กับวัคซีนรวม เหมาะกับน้องที่ออกนอกบ้าน ลุยน้ำ หรือเจอหนู โรคนี้ติดต่อถึงคนได้',
      },
      {
        name: 'โรคไอติดต่อในสุนัข',
        detail: 'สำหรับน้องที่ไปโรงแรมสัตว์ อาบน้ำตัดขน หรือเจอสุนัขตัวอื่นบ่อย',
      },
    ],
  },

  cat: {
    species: 'cat',
    label: 'น้องแมว',
    vaccines: [
      {
        id: 'core',
        name: 'วัคซีนรวมหลักของแมว',
        protects: 'ไข้หัดแมว และไข้หวัดแมวจากเชื้อเฮอร์ปีส์ไวรัสและคาลิซิไวรัส',
      },
      rabies,
    ],
    visits: [
      {
        id: 'series-1',
        title: 'วัคซีนรวมเข็มที่ 1',
        when: 'อายุ 8 สัปดาห์',
        timing: { kind: 'age-weeks', weeks: 8 },
        vaccineIds: ['core'],
      },
      {
        id: 'series-2',
        title: 'วัคซีนรวมเข็มที่ 2 และพิษสุนัขบ้าเข็มแรก',
        when: 'อายุ 12 สัปดาห์',
        timing: { kind: 'age-weeks', weeks: 12 },
        vaccineIds: ['core', 'rabies'],
        note: 'แมวก็ต้องฉีดพิษสุนัขบ้า กฎหมายกำหนดให้ฉีดครั้งแรกเมื่ออายุ 2 ถึง 4 เดือน',
      },
      {
        id: 'series-3',
        title: 'วัคซีนรวมเข็มที่ 3',
        when: 'อายุ 16 สัปดาห์',
        timing: { kind: 'age-weeks', weeks: 16 },
        vaccineIds: ['core'],
        note: 'เข็มสุดท้ายของชุดลูกแมว ควรฉีดเมื่ออายุ 16 สัปดาห์ขึ้นไป',
      },
      {
        id: 'first-booster',
        title: 'กระตุ้นครบ 1 ปี',
        when: 'อายุ 1 ปี',
        timing: { kind: 'age-months', months: 12 },
        vaccineIds: ['core', 'rabies'],
        note: 'สัตวแพทย์บางท่านนัดกระตุ้นเร็วขึ้นเมื่ออายุราว 6 เดือน',
      },
    ],
    repeat: {
      id: 'yearly',
      title: 'กระตุ้นประจำปี',
      when: 'ทุกปีหลังจากนั้น',
      timing: { kind: 'after-previous-months', months: 12 },
      vaccineIds: ['rabies', 'core'],
      note: 'พิษสุนัขบ้าฉีดทุกปีตามใบรับรองการฉีด ส่วนวัคซีนรวม สัตวแพทย์อาจให้เว้นเป็นทุก 3 ปีตามชนิดวัคซีนและการใช้ชีวิตของน้อง',
    },
    optional: [
      {
        name: 'ลิวคีเมียแมว',
        detail: 'สำหรับลูกแมว และแมวที่ออกนอกบ้านหรืออยู่ร่วมกับแมวตัวอื่น ควรตรวจเลือดก่อนฉีด',
      },
    ],
  },
};

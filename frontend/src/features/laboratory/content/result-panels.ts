import type { PanelAnalyte, PanelRange, ResultPanel } from '../types/result-panel.types';

/**
 * The result panels the "ลงผล" form fills itself from: the names, units and reference ranges of a
 * blood count, a ten-value chemistry and a urinalysis, for dogs and cats.
 *
 * MEDICAL CONTENT. A veterinarian at the clinic must check and approve this file before it is
 * used for real results, and again after every change. Fill in `clinicalReview` when they have;
 * until then the form says the ranges are awaiting the clinic vet's review. See
 * docs/features/clinical-records.md.
 *
 * What it is: widely published adult reference intervals in conventional units, of the kind
 * printed in veterinary clinical pathology references (the Merck Veterinary Manual's reference
 * tables for haematology, serum chemistry and urinalysis) and in the manuals of common in-clinic
 * analysers. They are a starting point to type against, NOT the clinic's ranges.
 *
 * Reference intervals differ between analysers, laboratories, methods, breeds and ages. The
 * range that counts is the one the clinic's own analyser or outside lab prints next to each
 * value. The vet must replace every number here with the intervals of the clinic's analyser, and
 * every range stays editable in the form for results from an outside lab.
 *
 * Species other than dog and cat get the names and units only, with the ranges left blank.
 */

/** Who approved these ranges and when. Leave both empty until a clinic vet has. */
export const clinicalReview: { reviewedBy?: string; reviewedOn?: string } = {};

const range = (low: number, high: number): PanelRange => ({ low, high });
const words = (text: string): PanelRange => ({ text });

const cbc: PanelAnalyte[] = [
  { analyte: 'WBC', unit: '10³/µL', ranges: { dog: range(6, 17), cat: range(5.5, 19.5) } },
  { analyte: 'RBC', unit: '10⁶/µL', ranges: { dog: range(5.5, 8.5), cat: range(5, 10) } },
  { analyte: 'HGB', unit: 'g/dL', ranges: { dog: range(12, 18), cat: range(8, 15) } },
  { analyte: 'HCT', unit: '%', ranges: { dog: range(37, 55), cat: range(24, 45) } },
  { analyte: 'MCV', unit: 'fL', ranges: { dog: range(60, 77), cat: range(39, 55) } },
  { analyte: 'MCH', unit: 'pg', ranges: { dog: range(19.5, 24.5), cat: range(12.5, 17.5) } },
  { analyte: 'MCHC', unit: 'g/dL', ranges: { dog: range(32, 36), cat: range(30, 36) } },
  { analyte: 'PLT', unit: '10³/µL', ranges: { dog: range(200, 500), cat: range(300, 800) } },
  { analyte: 'NEU', unit: '10³/µL', ranges: { dog: range(3, 11.5), cat: range(2.5, 12.5) } },
  { analyte: 'LYM', unit: '10³/µL', ranges: { dog: range(1, 4.8), cat: range(1.5, 7) } },
  { analyte: 'MONO', unit: '10³/µL', ranges: { dog: range(0.15, 1.35), cat: range(0, 0.85) } },
  { analyte: 'EOS', unit: '10³/µL', ranges: { dog: range(0.1, 1.25), cat: range(0, 1.5) } },
];

const chem10: PanelAnalyte[] = [
  { analyte: 'ALB', unit: 'g/dL', ranges: { dog: range(2.3, 4), cat: range(2.2, 4) } },
  { analyte: 'ALP', unit: 'U/L', ranges: { dog: range(23, 212), cat: range(14, 111) } },
  { analyte: 'ALT', unit: 'U/L', ranges: { dog: range(10, 125), cat: range(12, 130) } },
  { analyte: 'BUN', unit: 'mg/dL', ranges: { dog: range(7, 27), cat: range(16, 36) } },
  { analyte: 'CREA', unit: 'mg/dL', ranges: { dog: range(0.5, 1.8), cat: range(0.8, 2.4) } },
  { analyte: 'GLU', unit: 'mg/dL', ranges: { dog: range(70, 143), cat: range(74, 159) } },
  { analyte: 'TP', unit: 'g/dL', ranges: { dog: range(5.2, 8.2), cat: range(5.7, 8.9) } },
  { analyte: 'GLOB', unit: 'g/dL', ranges: { dog: range(2.5, 4.5), cat: range(2.8, 5.1) } },
  { analyte: 'TBIL', unit: 'mg/dL', ranges: { dog: range(0, 0.9), cat: range(0, 0.9) } },
  { analyte: 'CHOL', unit: 'mg/dL', ranges: { dog: range(110, 320), cat: range(65, 225) } },
];

const ua: PanelAnalyte[] = [
  { analyte: 'สี', unit: '', ranges: { dog: words('เหลือง'), cat: words('เหลือง') } },
  { analyte: 'ความขุ่น', unit: '', ranges: { dog: words('ใส'), cat: words('ใส') } },
  { analyte: 'USG', unit: '', ranges: { dog: range(1.015, 1.045), cat: range(1.035, 1.06) } },
  { analyte: 'pH', unit: '', ranges: { dog: range(5.5, 7.5), cat: range(5.5, 7.5) } },
  { analyte: 'Protein', unit: '', ranges: { dog: words('negative ถึง trace'), cat: words('negative ถึง trace') } },
  { analyte: 'Glucose', unit: '', ranges: { dog: words('negative'), cat: words('negative') } },
  { analyte: 'Ketones', unit: '', ranges: { dog: words('negative'), cat: words('negative') } },
  { analyte: 'Bilirubin', unit: '', ranges: { dog: words('negative ถึง 1+'), cat: words('negative') } },
  { analyte: 'Blood', unit: '', ranges: { dog: words('negative'), cat: words('negative') } },
];

/** In the order the form offers them. */
export const resultPanels: ResultPanel[] = [
  { id: 'cbc', label: 'CBC', codes: ['CBC'], names: ['cbc', 'complete blood count', 'ตรวจนับเม็ดเลือด', 'ความสมบูรณ์ของเม็ดเลือด'], analytes: cbc },
  {
    id: 'chem10',
    label: 'เคมีเลือด (CHEM10)',
    codes: ['CHEM10', 'CHEM 10', 'CHEM'],
    names: ['เคมีเลือด', 'chemistry', 'blood chemistry', 'chem10'],
    analytes: chem10,
  },
  { id: 'ua', label: 'ตรวจปัสสาวะ (UA)', codes: ['UA'], names: ['ตรวจปัสสาวะ', 'urinalysis', 'ua'], analytes: ua },
];

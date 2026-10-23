import type { PolicySection, PublicPrivacyPolicy } from '@/features/privacy/types/privacy-policy.types';
import type { StaffPolicyVersion } from '@/features/privacy/types/staff-privacy-policy.types';
import { table, type Row } from '../../core/db';
import { fail } from '../../core/http';
import { compact } from './store';

/** The privacy notice's versions (docs/contract-extensions.md § 35), as the console lists them. */

export type PolicyRow = Row & StaffPolicyVersion;

export const policies = () => table<PolicyRow>('privacy_policy_versions');

/** The label the code's old hard-coded notice used; a published version may not take it. */
export const LEGACY_LABEL = '2026-10';

export function currentPolicy(): PolicyRow | undefined {
  return policies().find((row) => row.status === 'published' && row.is_current);
}

/** What consents recorded now are stamped with. */
export function currentPolicyLabel(): string | undefined {
  return currentPolicy()?.label;
}

export function publishedPolicies(): PolicyRow[] {
  return policies()
    .where((row) => row.status === 'published')
    .sort((a, b) => (b.published_at ?? '').localeCompare(a.published_at ?? ''));
}

export function publicPolicy(row: PolicyRow | undefined): PublicPrivacyPolicy {
  const history = publishedPolicies().map((entry) => ({
    label: entry.label!,
    effective_date: entry.effective_date!,
    published_at: entry.published_at!,
  }));
  if (!row) return { published: false, history };
  return compact({
    published: true,
    label: row.label,
    effective_date: row.effective_date,
    published_at: row.published_at,
    legal_review_done: row.legal_review_done,
    is_current: row.is_current,
    sections: row.sections,
    history,
  });
}

const ANCHOR = /^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$/;
export const LABEL = /^[A-Za-z0-9][A-Za-z0-9._-]*$/;

/** The backend's `PrivacyPolicyRules.Problems`, answered as one `422` with every field's message. */
export function checkSections(input: unknown): PolicySection[] {
  if (!Array.isArray(input)) fail(422, 'validation_failed', 'ข้อมูลที่ส่งมาไม่ถูกต้อง', { details: { errors: { sections: ['ต้องส่งรายการหัวข้อ'] } } });
  const sections = (input as PolicySection[]).map((section) => ({
    anchor: String(section?.anchor ?? '').trim(),
    heading: String(section?.heading ?? '').trim(),
    body: String(section?.body ?? '').replace(/\r\n/g, '\n').trim(),
  }));
  const errors: Record<string, string[]> = {};
  if (sections.length === 0) errors.sections = ['ต้องมีอย่างน้อยหนึ่งหัวข้อ'];
  if (sections.length > 30) errors.sections = ['มีได้ไม่เกิน 30 หัวข้อ'];
  const seen = new Set<string>();
  sections.forEach((section, index) => {
    const at = `sections[${index}]`;
    if (!section.anchor) errors[`${at}.anchor`] = ['ต้องระบุชื่อจุดเชื่อม'];
    else if (section.anchor.length > 40 || !ANCHOR.test(section.anchor)) {
      errors[`${at}.anchor`] = ['ชื่อจุดเชื่อมต้องเป็นตัวพิมพ์เล็กภาษาอังกฤษ ตัวเลข และขีดกลาง ขึ้นต้นด้วยตัวอักษร ยาวไม่เกิน 40 ตัว เช่น data-we-collect'];
    } else if (section.anchor === 'controller') errors[`${at}.anchor`] = ['ชื่อ controller สงวนไว้สำหรับหัวข้อผู้ควบคุมข้อมูลที่ระบบสร้างให้'];
    else if (seen.has(section.anchor)) errors[`${at}.anchor`] = ['ชื่อจุดเชื่อมซ้ำกับหัวข้ออื่น'];
    seen.add(section.anchor);
    if (!section.heading) errors[`${at}.heading`] = ['ต้องระบุชื่อหัวข้อ'];
    else if (section.heading.length > 150) errors[`${at}.heading`] = ['ชื่อหัวข้อยาวได้ไม่เกิน 150 ตัวอักษร'];
    if (!section.body) errors[`${at}.body`] = ['ต้องมีเนื้อหา'];
    else if (section.body.length > 10_000) errors[`${at}.body`] = ['เนื้อหาหนึ่งหัวข้อยาวได้ไม่เกิน 10,000 ตัวอักษร'];
  });
  if (Object.keys(errors).length > 0) fail(422, 'validation_failed', 'ข้อมูลที่ส่งมาไม่ถูกต้อง', { details: { errors } });
  return sections;
}

'use server';

import { revalidatePath, revalidateTag } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { addHoliday, createBranch, replaceOpeningHours, requestBrandImage, updateBranch, updateClinicProfile } from '../api/clinic-setup';
import { CLINIC_PROFILE_TAG } from '../api/getCachedClinicProfile';
import type { BrandImageUpload } from '../types/settings.types';

/**
 * The clinic and hours settings pages' changes. The backend decides what is valid; these check the
 * shape, make one call, and mark stale every page that shows the result. The name, the hours and
 * the branches are on every public page, so the whole public site is stale with them.
 *
 * The profile and the week come back as stored, so a form that stays open after saving holds the
 * new version for its next save.
 */

function refresh(settingsPath: string) {
  revalidatePath(settingsPath);
  revalidatePath('/', 'layout');
}

/** The logo and tab icon every page reads from a shared, cached profile (§ 50): dropped at once on save. */
function refreshClinicMark() {
  revalidateTag(CLINIC_PROFILE_TAG);
}

const version = z.number().int().nonnegative();
const time = z.string().regex(/^\d{2}:\d{2}$/);

const profile = z.object({
  version,
  values: z.object({
    name: z.string().trim().max(200),
    logo_url: z.string().trim().max(1000),
    brand_color: z.string().trim().max(7),
    license_no: z.string().trim().max(64),
    operator_name: z.string().trim().max(200),
    tab_icon_url: z.string().trim().max(1000),
    show_name_with_logo: z.boolean(),
  }),
});

export async function updateClinicProfileAction(payload: z.infer<typeof profile>) {
  const parsed = profile.parse(payload);
  const result = await runAction(() => updateClinicProfile(parsed.version, parsed.values));

  refresh('/settings/clinic');
  refreshClinicMark();
  return result;
}

const brandImage = z.object({
  kind: z.enum(['logo', 'tab-icon']),
  contentType: z.enum(['image/png', 'image/jpeg', 'image/webp']),
  sizeBytes: z.number().int().positive().max(5 * 1024 * 1024),
});

/**
 * Asks for the signed URL of a new logo or tab icon (docs/contract-extensions.md § 50); the browser
 * then sends the file there itself. The website does not change until the profile is saved with the
 * value this answers.
 */
export async function requestBrandImageAction(values: z.infer<typeof brandImage>): Promise<ActionResult<BrandImageUpload>> {
  const input = brandImage.parse(values);

  return runAction(() => requestBrandImage(input.kind, { content_type: input.contentType, size_bytes: input.sizeBytes }));
}

const branch = z.object({
  code: z.string().trim().max(32),
  name: z.string().trim().max(200),
  address: z.string().trim().max(500).optional(),
  latitude: z.number().min(-90).max(90).optional(),
  longitude: z.number().min(-180).max(180).optional(),
  exam_rooms: z.array(z.string().trim().min(1).max(60)).max(50),
});

export async function createBranchAction(values: z.infer<typeof branch>) {
  const parsed = branch.parse(values);
  const result = await runAction(() => createBranch(parsed));

  refresh('/settings/clinic');
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const branchUpdate = z.object({
  id: z.string().trim().min(1).max(40),
  version,
  values: z.object({
    name: z.string().trim().max(200),
    address: z.string().trim().max(500),
    latitude: z.number().min(-90).max(90).optional(),
    longitude: z.number().min(-180).max(180).optional(),
    exam_rooms: z.array(z.string().trim().min(1).max(60)).max(50),
  }),
});

/** Changes a branch (docs/contract-extensions.md § 51). Its name and address are on public pages. */
export async function updateBranchAction(payload: z.infer<typeof branchUpdate>) {
  const parsed = branchUpdate.parse(payload);
  const result = await runAction(() => updateBranch(parsed.id, parsed.version, parsed.values));

  refresh('/settings/clinic');
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const week = z.object({
  version,
  days: z
    .array(
      z.object({
        day: z.enum(['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']),
        opens: time.optional(),
        closes: time.optional(),
        closed: z.boolean(),
      }),
    )
    .length(7),
});

export async function replaceOpeningHoursAction(payload: z.infer<typeof week>) {
  const parsed = week.parse(payload);
  const result = await runAction(() => replaceOpeningHours(parsed.version, parsed.days));

  refresh('/settings/hours');
  return result;
}

const holiday = z.object({
  date: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  name: z.string().trim().max(200),
  branch_id: z.string().trim().max(40).optional(),
});

export async function addHolidayAction(values: z.infer<typeof holiday>) {
  const parsed = holiday.parse(values);
  const result = await runAction(() => addHoliday(parsed));

  refresh('/settings/hours');
  return result.ok ? { ok: true as const, data: undefined } : result;
}

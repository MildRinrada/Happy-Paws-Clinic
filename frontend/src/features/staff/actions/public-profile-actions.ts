'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { replacePublicProfile, requestStaffPhoto } from '../api/manage-staff';
import type { StaffPhotoUpload } from '../types/staff.types';

/** Lengths mirror the backend's; whether a photo path or a year is acceptable is its answer. */
const profile = z.object({
  id: z.string().min(1).max(64),
  version: z.number().int().nonnegative(),
  values: z.object({
    job_title: z.string().trim().max(120),
    specialty: z.string().trim().max(120),
    bio: z.string().trim().max(600),
    photo_path: z.string().trim().max(300),
    career_start_year: z.number().int().min(1900).max(2200).optional(),
    show_on_website: z.boolean(),
  }),
});

/** Replaces a member's public profile. Blank text is sent as absent, which clears that field. */
export async function replacePublicProfileAction(payload: z.infer<typeof profile>) {
  const { id, version, values } = profile.parse(payload);
  const result = await runAction(() =>
    replacePublicProfile(id, version, {
      job_title: values.job_title || undefined,
      specialty: values.specialty || undefined,
      bio: values.bio || undefined,
      photo_path: values.photo_path || undefined,
      career_start_year: values.career_start_year,
      show_on_website: values.show_on_website,
    }),
  );

  revalidatePath('/settings/staff');
  revalidatePath('/team');
  revalidatePath('/');
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const photo = z.object({
  id: z.string().min(1).max(64),
  contentType: z.enum(['image/jpeg', 'image/png', 'image/webp']),
  sizeBytes: z.number().int().positive().max(5 * 1024 * 1024),
});

/**
 * Asks for the signed URL of a new photo (§ 32); the browser then sends the file there itself.
 * The website does not change until the profile is saved with the `photo_path` this answers.
 */
export async function requestStaffPhotoAction(values: z.infer<typeof photo>): Promise<ActionResult<StaffPhotoUpload>> {
  const input = photo.parse(values);

  return runAction(() => requestStaffPhoto(input.id, { content_type: input.contentType, size_bytes: input.sizeBytes }));
}

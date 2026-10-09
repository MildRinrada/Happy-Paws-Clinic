'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { replacePublicProfile } from '../api/manage-staff';

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

'use client';

/* eslint-disable @next/next/no-img-element -- photo_url is a short-lived signed link from the API, not a file next/image can optimise. */
import { useState } from 'react';
import { ImageSlot, type ImageSlotTone } from '@/components/shared/image-slot';
import type { TeamMember } from '../types/team.types';

interface MemberPhotoProps {
  member: Pick<TeamMember, 'display_name' | 'photo_path' | 'photo_url'>;
  /** Says what picture belongs here when there is none. */
  label: string;
  sizes: string;
  /** Size and shape of the frame. It must give the frame a height. */
  className: string;
  /** How the empty frame is filled. The site's light grey when left out. */
  tone?: ImageSlotTone;
}

/**
 * A person's photo on a public page: the one staff uploaded (a signed link that works for an hour,
 * minted for this page, docs/contract-extensions.md § 32), else a file under `/public`, else the
 * empty frame. A link that stops working, say on a page left open past the hour, falls back the
 * same way instead of showing a broken picture.
 */
export function MemberPhoto({ member, label, sizes, className, tone }: MemberPhotoProps) {
  const [failed, setFailed] = useState(false);

  if (member.photo_url && !failed) {
    return (
      <div className={`relative ${className}`}>
        <img
          src={member.photo_url}
          alt={member.display_name}
          loading="lazy"
          className="absolute inset-0 size-full object-cover"
          onError={() => setFailed(true)}
        />
      </div>
    );
  }

  return (
    <ImageSlot src={member.photo_path} alt={member.display_name} label={label} sizes={sizes} tone={tone} className={className} />
  );
}

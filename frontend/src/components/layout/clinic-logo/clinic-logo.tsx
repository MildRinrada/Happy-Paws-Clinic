'use client';

/* eslint-disable @next/next/no-img-element -- the logo is the clinic's own upload or address, behind a redirect to a short-lived signed link; next/image would cache what must not be cached. */
import { useState } from 'react';
import { LogoMark } from '../site-header/logo-mark';
import { useClinicIdentity, useClinicNameShown } from './clinic-identity-context';

interface ClinicLogoProps {
  /** The paw's box, exactly as each place drew it before a logo could be set, e.g. `size-10`. */
  markClassName: string;
  /** The logo's height (the paw's) and the widest it may grow beside the name, e.g. `h-10 max-w-[150px]`. */
  imageClassName: string;
  /** The same when the name is not written beside it and the logo has the room to itself. */
  aloneImageClassName?: string;
}

/**
 * The clinic's mark (docs/contract-extensions.md § 50): its own logo when it has set one, else the
 * stand-in paw, drawn exactly as before. A logo of any shape keeps the paw's height, grows as wide
 * as it needs up to the place's limit, and is fitted inside without cropping.
 *
 * When the name is not written beside it, the logo itself carries the clinic's name for screen
 * readers; otherwise it is decoration beside the name. A logo that fails to load falls back to the
 * paw rather than a broken picture.
 */
export function ClinicLogo({ markClassName, imageClassName, aloneImageClassName = imageClassName }: ClinicLogoProps) {
  const identity = useClinicIdentity();
  const nameShown = useClinicNameShown();
  const [failed, setFailed] = useState<string>();

  if (!identity.logoSrc || failed === identity.logoSrc) {
    return <LogoMark className={markClassName} />;
  }

  return (
    <img
      src={identity.logoSrc}
      alt={nameShown ? '' : identity.name}
      className={`w-auto shrink-0 rounded-lg object-contain ${nameShown ? imageClassName : aloneImageClassName}`}
      onError={() => setFailed(identity.logoSrc)}
    />
  );
}

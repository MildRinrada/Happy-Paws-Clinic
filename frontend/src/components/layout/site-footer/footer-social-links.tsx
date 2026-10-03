import type { ComponentType } from 'react';
import type { SocialIconName, SocialLink } from '@/config/site-footer';
import { FacebookIcon } from './icons/facebook-icon';
import { InstagramIcon } from './icons/instagram-icon';
import { LineIcon } from './icons/line-icon';

const icons: Record<SocialIconName, ComponentType<{ className?: string }>> = {
  instagram: InstagramIcon,
  line: LineIcon,
  facebook: FacebookIcon,
};

/** The clinic's chat channels, as a row of app icons. */
export function FooterSocialLinks({ label, links }: { label: string; links: SocialLink[] }) {
  return (
    <div>
      <p className="text-[13px]">{label}</p>
      <ul className="mt-2 flex gap-2.5">
        {links.map((link) => {
          const Icon = icons[link.icon];

          return (
            <li key={link.name}>
              <a href={link.href} aria-label={link.name} className="block transition hover:opacity-85">
                <Icon className="size-10" />
              </a>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

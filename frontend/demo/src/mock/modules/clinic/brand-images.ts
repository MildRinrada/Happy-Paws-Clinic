import { MOCK_API } from '../../../../mock-api';
import { fileUrl } from '../../core/files';

/**
 * An uploaded logo or tab icon is drawn from `GET /clinic/logo?v=<file>` (brand-image-src.ts). The
 * product puts that address straight into `<img src>` and `<link rel="icon" href>`, which the
 * browser loads itself, past the fetch the demo answers. So the address is swapped for the
 * uploaded file's data URL as it is set, before the browser starts loading it: React sets `src`
 * through `setAttribute`, and a few places assign the property.
 */

const PREFIX = `${MOCK_API}/clinic/`;
const PATTERN = /\/clinic\/(?:logo|tab-icon)\?v=(?:logo-|tab-icon-)?([^&]+)/;

function resolve(value: unknown): unknown {
  if (typeof value !== 'string' || !value.startsWith(PREFIX)) return value;
  const found = PATTERN.exec(value);
  return (found && fileUrl(decodeURIComponent(found[1]!))) || value;
}

if (typeof window !== 'undefined') {
  const setAttribute = Element.prototype.setAttribute;
  Element.prototype.setAttribute = function (name: string, value: string) {
    const swapped = (name === 'src' || name === 'href') && (this instanceof HTMLImageElement || this instanceof HTMLLinkElement) ? resolve(value) : value;
    return setAttribute.call(this, name, swapped as string);
  };

  for (const [type, property] of [[HTMLImageElement, 'src'], [HTMLLinkElement, 'href']] as const) {
    const descriptor = Object.getOwnPropertyDescriptor(type.prototype, property);
    if (!descriptor?.set || !descriptor.get) continue;
    Object.defineProperty(type.prototype, property, {
      ...descriptor,
      set(value: string) {
        descriptor.set!.call(this, resolve(value) as string);
      },
    });
  }
}

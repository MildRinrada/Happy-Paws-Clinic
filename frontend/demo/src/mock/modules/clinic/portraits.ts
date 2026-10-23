/**
 * Drawn portraits for the sample staff, who are made-up people: a photo of a real person under an
 * invented name would be a lie, an empty frame looks unfinished. Each is a 4:5 SVG (the team card's
 * shape) built from the member's department, a guess at how they dress from their title, and a
 * number from their name, so the same person always gets the same picture.
 */

interface Look {
  background: string;
  /** Scrubs or shirt. */
  top: string;
  /** A white coat over the top, with a stethoscope: the vets. */
  coat: boolean;
}

const LOOKS: Record<string, Look> = {
  veterinary: { background: '#d5f2ec', top: '#217a71', coat: true },
  nursing: { background: '#dbeafe', top: '#3b6fb6', coat: false },
  front_desk: { background: '#fde8d4', top: '#e07b39', coat: false },
  finance: { background: '#ede7fb', top: '#6d5bb8', coat: false },
  management: { background: '#efede9', top: '#2e2b27', coat: false },
  support: { background: '#e6f4d9', top: '#4f8a3a', coat: false },
};

const SKIN = ['#f3cfae', '#ebbd96', '#e0ab82', '#cf9670'];
const HAIR = ['#1c1714', '#2a1f1a', '#3a2a20', '#241b16'];

function hash(text: string): number {
  let value = 2166136261;
  for (const char of text) value = Math.imul(value ^ char.codePointAt(0)!, 16777619);
  return value >>> 0;
}

/** Thai titles say it outright; for the rest, the endings Thai men's names usually have. */
function isMan(name: string): boolean {
  if (name.startsWith('น.สพ.') || name.startsWith('นาย')) return true;
  if (name.startsWith('สพ.ญ.') || name.startsWith('นาง')) return false;
  const first = name.split(' ')[0] ?? '';
  return /(วุฒิ|ชาย|พงศ์|พงษ์|กร|กิตติ|ศักดิ์|ชัย|ธนา|เดช|ภพ)$/.test(first);
}

function hairBack(man: boolean, style: number, color: string): string {
  if (man) return '';
  // Long hair falls behind the shoulders; a bob stops at the jaw; a bun has nothing behind.
  if (style === 0) return `<path d="M128 190 C120 300 130 360 150 390 L250 390 C270 360 280 300 272 190 Z" fill="${color}"/>`;
  if (style === 1) return `<path d="M116 184 C106 252 114 300 136 318 L264 318 C286 300 294 252 284 184 Z" fill="${color}"/>`;
  return '';
}

function hairFront(man: boolean, style: number, color: string): string {
  if (man) {
    return style === 0
      ? `<path d="M140 196 C136 140 170 116 202 118 C236 120 264 140 260 196 C254 170 236 156 200 156 C170 156 148 168 140 196 Z" fill="${color}"/>`
      : `<path d="M138 200 C132 136 176 112 206 116 C240 120 266 146 262 196 C250 176 232 164 214 160 C190 168 160 166 138 200 Z" fill="${color}"/>`;
  }
  const crown = `<path d="M136 206 C130 136 172 112 202 114 C236 116 270 142 264 206 C256 172 230 152 200 152 C172 152 146 170 136 206 Z" fill="${color}"/>`;
  return style === 2 ? `<circle cx="200" cy="88" r="28" fill="${color}"/>${crown}` : crown;
}

function torso(look: Look, skin: string): string {
  const neck = `<path d="M178 262 L178 300 C186 312 214 312 222 300 L222 262 Z" fill="${skin}"/>`;
  const body = `<path d="M70 500 C72 400 110 330 170 310 L200 334 L230 310 C290 330 328 400 330 500 Z" fill="${look.top}"/>`;
  const vNeck = `<path d="M170 310 L200 352 L230 310 L222 304 L200 336 L178 304 Z" fill="${skin}"/>`;
  if (!look.coat) return neck + body + vNeck;
  const coat =
    `<path d="M70 500 C72 400 108 334 166 312 L192 400 L176 500 Z" fill="#ffffff"/>` +
    `<path d="M330 500 C328 400 292 334 234 312 L208 400 L224 500 Z" fill="#ffffff"/>` +
    `<path d="M166 312 L192 400 M234 312 L208 400" stroke="#d8d3cc" stroke-width="3" fill="none"/>`;
  const stethoscope =
    `<path d="M172 318 C150 370 160 420 196 424 C232 420 246 370 228 318" stroke="#2e2b27" stroke-width="6" fill="none" stroke-linecap="round"/>` +
    `<circle cx="196" cy="428" r="11" fill="#9c958a" stroke="#2e2b27" stroke-width="4"/>`;
  return neck + body + vNeck + coat + stethoscope;
}

function face(man: boolean, skin: string, glasses: boolean, cheek: string): string {
  const head = `<ellipse cx="200" cy="208" rx="62" ry="72" fill="${skin}"/>`;
  const ears = `<ellipse cx="138" cy="214" rx="11" ry="16" fill="${skin}"/><ellipse cx="262" cy="214" rx="11" ry="16" fill="${skin}"/>`;
  const brows = man
    ? `<path d="M164 186 Q178 180 190 186 M210 186 Q222 180 236 186" stroke="#2a1f1a" stroke-width="5" fill="none" stroke-linecap="round"/>`
    : `<path d="M166 186 Q178 181 189 185 M211 185 Q222 181 234 186" stroke="#3a2a20" stroke-width="3.5" fill="none" stroke-linecap="round"/>`;
  const eyes = `<ellipse cx="177" cy="208" rx="5.5" ry="7" fill="#231c18"/><ellipse cx="223" cy="208" rx="5.5" ry="7" fill="#231c18"/>`;
  const blush = man ? '' : `<ellipse cx="160" cy="236" rx="11" ry="6" fill="${cheek}" opacity="0.45"/><ellipse cx="240" cy="236" rx="11" ry="6" fill="${cheek}" opacity="0.45"/>`;
  const nose = `<path d="M200 214 Q195 232 202 234" stroke="#b97d5c" stroke-width="3" fill="none" stroke-linecap="round"/>`;
  const smile = `<path d="M180 248 Q200 266 220 248" stroke="#8a4b3a" stroke-width="5" fill="none" stroke-linecap="round"/>`;
  const frames = glasses
    ? `<g fill="none" stroke="#2e2b27" stroke-width="4"><rect x="157" y="194" width="38" height="28" rx="10"/><rect x="205" y="194" width="38" height="28" rx="10"/><path d="M195 206 L205 206"/></g>`
    : '';
  return head + ears + brows + eyes + blush + nose + smile + frames;
}

export function portraitSvg(member: { display_name: string; department?: string; id?: string }): string {
  const seed = hash(member.display_name);
  const look = LOOKS[member.department ?? ''] ?? LOOKS.management!;
  const man = isMan(member.display_name);
  const skin = SKIN[seed % SKIN.length]!;
  const hair = HAIR[(seed >>> 3) % HAIR.length]!;
  const style = (seed >>> 6) % 3;
  const glasses = (seed >>> 9) % 4 === 0;
  return (
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 500" preserveAspectRatio="xMidYMin slice">` +
    `<rect width="400" height="500" fill="${look.background}"/>` +
    `<circle cx="320" cy="80" r="120" fill="#ffffff" opacity="0.35"/>` +
    // Drawn on a 400×500 grid, enlarged and set a little lower so the head has room above it. A wide
    // frame (the home page's vet cards) keeps the top and loses the bottom, so the face always shows.
    `<g transform="translate(200 540) scale(1.12) translate(-200 -500)">` +
    hairBack(man, style, hair) +
    torso(look, skin) +
    face(man, skin, glasses, '#e8857a') +
    hairFront(man, style, hair) +
    `</g>` +
    `</svg>`
  );
}

export function portraitUrl(member: { display_name: string; department?: string; id?: string }): string {
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(portraitSvg(member))}`;
}

/**
 * The team banner: the sample staff as a group picture filling the whole frame, the clinic's sign
 * across the top. Up to seven people, the vets in the middle and in front.
 */
export function teamBannerUrl(members: Array<{ display_name: string; department?: string; id?: string }>): string {
  const order = [...members].sort((a, b) => Number(b.department === 'veterinary') - Number(a.department === 'veterinary')).slice(0, 7);
  // Middle first, then out to each side, so the vets stand at the centre.
  const slots = [3, 2, 4, 1, 5, 0, 6];
  // Drawn back to front: the outermost first, so the middle overlaps them.
  const placed = order.map((member, index) => ({ member, slot: slots[index]! })).sort((a, b) => Math.abs(b.slot - 3) - Math.abs(a.slot - 3));
  const busts = placed
    .map(({ member, slot }) => {
      const inner = portraitSvg(member)
        .replace(/^<svg[^>]*>/, '')
        .replace(/<\/svg>$/, '')
        .replace(/<rect width="400" height="500"[^>]*\/>/, '')
        .replace(/<circle cx="320" cy="80" r="120"[^>]*\/>/, '');
      const back = Math.abs(slot - 3);
      const scale = 0.98 - back * 0.07;
      // Seven places across 1100 wide; each figure stands on the bottom edge, the back row a little higher.
      const centre = 100 + slot * 150;
      const x = centre - 200 * scale;
      const y = 765 - 540 * scale - back * 18;
      return `<g transform="translate(${x.toFixed(1)} ${y.toFixed(1)}) scale(${scale.toFixed(3)})">${inner}</g>`;
    })
    .join('');
  const svg =
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1100 765" preserveAspectRatio="xMidYMax slice">` +
    `<defs><linearGradient id="room" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#eefaf7"/><stop offset="1" stop-color="#aee4da"/></linearGradient></defs>` +
    `<rect width="1100" height="765" fill="url(#room)"/>` +
    // The clinic's wall behind them: its sign, and two windows either side.
    `<rect width="1100" height="118" fill="#0e9384"/>` +
    `<text x="550" y="78" text-anchor="middle" font-family="Inter, Arial, sans-serif" font-size="52" font-weight="700" fill="#ffffff">Happy Paws Clinic</text>` +
    `<rect x="40" y="160" width="220" height="200" rx="14" fill="#ffffff" opacity="0.7"/>` +
    `<rect x="840" y="160" width="220" height="200" rx="14" fill="#ffffff" opacity="0.7"/>` +
    busts +
    `</svg>`;
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}

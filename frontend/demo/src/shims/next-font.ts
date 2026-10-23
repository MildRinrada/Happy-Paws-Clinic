/**
 * `next/font/google`. The demo loads its fonts from index.html and sets the same CSS variables in
 * src/styles.css, so a font function only has to hand back empty class names.
 */
function font() {
  return { className: '', variable: '', style: { fontFamily: 'inherit' } };
}

export const IBM_Plex_Sans_Thai = font;
export const Prompt = font;
export const Inter = font;

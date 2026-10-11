'use client';

import { useEffect } from 'react';

/** True when the key press belongs to something the person is typing into. */
function isTyping(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  if (target.isContentEditable) return true;
  if (target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement) return true;

  return target instanceof HTMLInputElement && !['checkbox', 'radio', 'button', 'submit', 'reset'].includes(target.type);
}

/**
 * `/`, or Ctrl+K (⌘K on a Mac), from anywhere in the console puts the cursor in the search box.
 * Never while typing in another field, where `/` is a character someone meant and Ctrl+K belongs
 * to that field.
 */
export function useSearchShortcut(onOpen: () => void) {
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.defaultPrevented || event.isComposing || event.altKey) return;

      const slash = event.key === '/' && !event.ctrlKey && !event.metaKey;
      const commandK = (event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k';

      if ((!slash && !commandK) || isTyping(event.target)) return;

      event.preventDefault();
      onOpen();
    };

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [onOpen]);
}

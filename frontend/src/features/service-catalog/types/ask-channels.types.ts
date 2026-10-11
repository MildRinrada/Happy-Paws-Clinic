/** The clinic's phone and LINE, for "โทรสอบถาม" and the closing band's buttons. Absent when not set. */
export interface AskChannels {
  phone?: { number: string; href: string };
  line?: { href: string };
}

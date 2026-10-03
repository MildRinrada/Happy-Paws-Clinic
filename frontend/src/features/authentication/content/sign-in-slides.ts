/**
 * The photo slideshow beside the sign-in form. Edit, add or remove slides here.
 *
 * Pictures: put the file in /public and set `image` to its path, e.g.
 * '/images/sign-in/vet-with-pets.jpg'. A slide with no image shows the clinic's green instead,
 * with `imageLabel` on it so whoever fills the page in knows what belongs there.
 *
 * Headings and body text are one string per line. Thai has no spaces between words to break at,
 * so the lines are set here rather than left to the browser.
 *
 * The current photos are from Unsplash, under the Unsplash License (free to use, no credit
 * required). Sources, should anyone need them:
 *   vet-exam-dog.jpg        https://unsplash.com/photos/u2H8mUzoF2Q
 *   owner-phone-dog.jpg     https://unsplash.com/photos/HPQ0seAY16U
 *   cat-in-carrier.jpg      https://unsplash.com/photos/OBu_nabEpAw
 *   vet-injection-dog.jpg   https://unsplash.com/photos/fQeLC7WlNm8
 */

export interface SignInSlide {
  image?: string;
  imageAlt: string;
  imageLabel: string;
  /**
   * Which part of the photo to keep when the panel crops it, as CSS `object-position`. The panel
   * is taller than it is wide, so a landscape photo loses its sides. Defaults to the centre.
   */
  focus?: string;
  heading: string[];
  body: string[];
}

export const signInShowcase = {
  /** The small line above every slide's heading. */
  tagline: 'CARING FOR EVERY PAW',
  footer: 'Happy Paws Clinic ดูแลน้องด้วยใจ ทุกวัน',
  carouselLabel: 'ภาพแนะนำคลินิก',
  slideLabel: (number: number) => `สไลด์ที่ ${number}`,
  /** How long each slide stays before the next, in milliseconds. */
  intervalMs: 6000,
};

export const signInSlides: SignInSlide[] = [
  {
    image: '/images/sign-in/vet-exam-dog.jpg',
    imageAlt: 'สุนัขชิวาวายืนบนเตียงตรวจ มีมือของผู้ช่วยสัตวแพทย์ประคองอยู่',
    imageLabel: 'รูปสุนัขบนเตียงตรวจ',
    heading: ['ประวัติสุขภาพของน้อง', 'อยู่กับคุณตลอด'],
    body: ['เข้าสู่ระบบเพื่อจองนัด ติดตามคิวสด', 'และดูผลตรวจย้อนหลังของน้องทุกตัวในบัญชีเดียว'],
  },
  {
    image: '/images/sign-in/owner-phone-dog.jpg',
    imageAlt: 'ผู้หญิงกอดสุนัขชิบะริมหน้าต่าง และถือโทรศัพท์อยู่',
    imageLabel: 'รูปเจ้าของกับสุนัขและโทรศัพท์',
    focus: '38% center',
    heading: ['จองนัดได้ทุกเวลา', 'ไม่ต้องรอสาย'],
    body: ['เลือกวัน เวลา และคุณหมอที่สะดวก', 'ได้จากที่บ้าน'],
  },
  {
    image: '/images/sign-in/cat-in-carrier.jpg',
    imageAlt: 'แมวสีขาวนอนรออยู่ในกระเป๋าใส่สัตว์แบบใส',
    imageLabel: 'รูปแมวในกระเป๋าใส่สัตว์',
    heading: ['รู้ว่าถึงคิวเมื่อไร', 'ไม่ต้องนั่งรอหน้าห้อง'],
    body: ['ติดตามคิวจากโทรศัพท์', 'แล้วมาให้ทันตอนใกล้ถึงคิวของน้อง'],
  },
  {
    image: '/images/sign-in/vet-injection-dog.jpg',
    imageAlt: 'สัตวแพทย์สวมถุงมือกำลังฉีดยาที่ขาหน้าของสุนัข',
    imageLabel: 'รูปสัตวแพทย์ฉีดยาให้สุนัข',
    heading: ['ไม่พลาดวัคซีน', 'ทุกเข็มของน้อง'],
    body: ['ดูวันครบกำหนดวัคซีนและนัดครั้งถัดไป', 'ของน้องทุกตัวได้ในที่เดียว'],
  },
];

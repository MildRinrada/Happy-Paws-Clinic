# SEO ของเว็บสาธารณะ

สิ่งที่ทำไว้ให้ Google และแอปแชตเข้าใจเว็บของคลินิก เพื่อให้คนค้นเจอ และลิงก์ที่แชร์ใน LINE หรือ Facebook มีรูปและคำอธิบาย ทุกอย่างอยู่ฝั่งหน้าเว็บ ไม่ได้แก้ backend

## ตั้งค่าก่อนเปิดใช้จริง

| ตัวแปร | ค่าตัวอย่าง | ความหมาย |
|---|---|---|
| `NEXT_PUBLIC_SITE_URL` | `https://www.happypaws.example` | ที่อยู่จริงของเว็บ ไม่มี `/` ท้าย ลิงก์ canonical รูปตัวอย่าง sitemap robots.txt และข้อมูลโครงสร้างสร้างจากค่านี้ |

- ถ้าไม่ตั้ง ใช้ `http://localhost:3000` ซึ่งใช้ได้แค่ตอนพัฒนา เปิดใช้จริงโดยไม่ตั้งค่านี้ Google จะเห็นลิงก์ที่ชี้ localhost
- Next ฝังค่านี้ตอน build ต้องตั้งก่อน `next build` เปลี่ยนแล้วต้อง build ใหม่
- ตอนพัฒนาใส่ใน `frontend/.env.local` ตอนใช้ Docker ใส่ใน `.env` ที่ root แล้ว `docker compose build web` ใหม่ (`docker-compose.yml` ส่งเป็น build arg)

ข้อมูลคลินิกที่อยู่ในข้อมูลโครงสร้างต้องเป็นของจริงก่อนเปิดใช้ ดูหัวข้อ [ข้อมูลที่ต้องเติมในระบบ](#ข้อมูลที่ต้องเติมในระบบ)

## มีอะไรบ้าง

### metadata ทุกหน้า

ทุกหน้าใต้ `app/(public)` ใช้ `pageMetadata()` ซึ่งให้

- `title` และ `description`
- ลิงก์ canonical ของหน้านั้น
- Open Graph (ชื่อ คำอธิบาย URL ชื่อเว็บ ภาษา `th_TH` และรูป)
- X (Twitter) แบบ `summary_large_image` ใช้ชื่อ คำอธิบาย และรูปเดียวกับ Open Graph

| หน้า | ชื่อและคำอธิบายอยู่ที่ |
|---|---|
| `/` | `homeMeta` ใน `features/seo/content/seo.ts` |
| `/services` | `servicesMeta` ใน `features/service-catalog/content/services-page.ts` |
| `/vaccine-schedule` | `vaccinePage.meta` ใน `features/vaccine-schedule/content/vaccine-schedule-page.ts` |
| `/about` | `aboutMeta` ใน `features/about/content/about-page.ts` |
| `/team` | `teamPage` ใน `features/team/content/team-page.ts` |
| `/contact` | `contactPage.meta` ใน `features/contact/content/contact-page.ts` |
| `/privacy` | `privacyMeta` ใน `features/privacy/content/privacy-policy.ts` |
| `/articles` | ในไฟล์หน้า `app/(public)/articles/page.tsx` |
| `/articles/<slug>` | ชื่อและสรุปของบทความ ชนิด `article` พร้อมวันที่เผยแพร่ |

`metadataBase` ตั้งใน `app/layout.tsx` จาก `NEXT_PUBLIC_SITE_URL` ลิงก์และรูปที่เขียนเป็น path จึงกลายเป็น URL เต็ม

### รูปตัวอย่างเวลาแชร์ลิงก์

- รูปกลางของเว็บ 1200 x 630 สร้างจาก `app/(public)/opengraph-image.tsx` ตอน build ได้ที่ `/opengraph-image` พื้นเขียวเข้ม อุ้งเท้าเดียวกับไอคอนแท็บ ชื่อคลินิก และข้อความภาษาไทย ข้อความแก้ที่ `ogImage` ใน `features/seo/content/seo.ts`
- ฟอนต์ภาษาไทยในรูปคือ Prompt ไฟล์อยู่ที่ `frontend/src/assets/fonts/` สัญญาอนุญาต SIL Open Font License (`OFL.txt` ในโฟลเดอร์เดียวกัน) ฟอนต์ในตัวของ Next ไม่มีตัวอักษรไทย
- บทความที่มีรูปปกเป็นไฟล์ใต้ `public/` (`cover_image_path`) ใช้รูปปกนั้น บทความที่ปกเป็นรูปที่พนักงานอัปโหลดใช้รูปกลางแทน เพราะลิงก์รูปอัปโหลดหมดอายุในหนึ่งชั่วโมง แต่ LINE และ Facebook เก็บตัวอย่างไว้นานกว่านั้น

### sitemap.xml และ robots.txt

- `/sitemap.xml` (`app/sitemap.ts`) มีหน้าแรก บริการและราคา ตารางวัคซีน บทความ เกี่ยวกับคลินิก ทีมของเรา ติดต่อเรา นโยบายความเป็นส่วนตัว และบทความที่เผยแพร่แล้ว 50 เรื่องล่าสุด (สูงสุดที่ `GET /articles` ให้ต่อครั้ง) บทความมี `lastModified` เป็นวันที่เผยแพร่ สร้างใหม่ทุกครั้งที่ถูกเรียก ถ้า API ไม่ตอบ sitemap ยังมีหน้าหลัก แค่ไม่มีบทความ
- `/robots.txt` (`app/robots.ts`) ให้เก็บทุกหน้า ยกเว้นหน้าเข้าสู่ระบบและสมัคร portal ลูกค้า หน้าจอพนักงานทุกหน้า จอคิว และ `/api/` แล้วบอกที่อยู่ sitemap เพิ่มหน้าจอพนักงานใหม่ใต้ `app/(staff)` ให้เพิ่ม path ใน `PRIVATE_PATHS` ด้วย

### ข้อมูลโครงสร้าง (JSON-LD)

| หน้า | ชนิด | ข้อมูลมาจาก |
|---|---|---|
| `/` | `VeterinaryCare`, `WebSite` | ดูตารางถัดไป |
| `/contact` | `VeterinaryCare`, `BreadcrumbList` | ดูตารางถัดไป |
| `/services` | `OfferCatalog`, `BreadcrumbList` | `GET /services`, `GET /services/categories` |
| `/articles/<slug>` | `Article`, `BreadcrumbList` | `GET /articles/{slug}` และชื่อกับโลโก้จาก `GET /clinic` |
| `/about`, `/team`, `/articles`, `/privacy` | `BreadcrumbList` | ชื่อหน้าใน `sitePages` (`features/seo/content/seo.ts`) |

`VeterinaryCare` (LocalBusiness ชนิดคลินิกสัตว์) สร้างจากข้อมูลในระบบเท่านั้น ช่องที่คลินิกยังไม่ได้ใส่จะไม่มีในผลลัพธ์

| ช่อง | มาจาก |
|---|---|
| `name`, `logo` | `GET /clinic` (ถ้าอ่านไม่ได้ใช้ชื่อ Happy Paws Clinic) |
| `address`, `geo` | สาขาจาก `GET /clinic/branches` ถ้ามีสาขาเดียว ใช้ที่อยู่และพิกัดของสาขานั้น ถ้ามีหลายสาขา ใช้ที่อยู่ของรายการแผนที่ที่เป็นของทั้งคลินิก และใส่แต่ละสาขาเป็น `department` พร้อมที่อยู่ พิกัด และเบอร์ของสาขา |
| `telephone` | รายการโทรศัพท์แรกของทั้งคลินิกใน `GET /clinic/contacts` |
| `email` | รายการอีเมลแรก |
| `contactPoint` | สายด่วนฉุกเฉินรายการแรก |
| `hasMap` | ลิงก์ของรายการแผนที่ |
| `sameAs` | ลิงก์ LINE Facebook Instagram และเว็บไซต์ของทั้งคลินิก |
| `openingHoursSpecification` | เวลาทำการรายสัปดาห์จาก `GET /clinic/hours` วันที่เวลาเหมือนกันรวมเป็นรายการเดียว วันปิดไม่ใส่ ถ้าคลินิกยังไม่ได้ตั้งเวลาทำการจะไม่มีช่องนี้ |
| `specialOpeningHoursSpecification` | วันหยุดพิเศษของทั้งคลินิกที่ยังมาไม่ถึง เขียนเป็นเปิด 00:00 ปิด 00:00 ตามแบบของ schema.org |

ที่อยู่ใน API เป็นข้อความบรรทัดเดียว จึงใส่ทั้งบรรทัดใน `streetAddress` และใส่ประเทศเป็น `TH` เสมอ ตัวเลขบนหน้าแรก (จำนวนสัตว์ คะแนนรีวิว จำนวนปี) เป็นตัวเลขจากดีไซน์ที่ยังไม่ได้ยืนยัน จึงไม่ได้ใส่ในข้อมูลโครงสร้าง ไม่มี `FAQPage` เพราะเว็บยังไม่มีหน้าคำถามที่พบบ่อย

`OfferCatalog` บนหน้าบริการ แบ่งตามหมวด บริการละหนึ่ง `Offer` ราคาเดียวใช้ `price` ราคาที่แยกตามชนิดหรือน้ำหนักใช้ `PriceSpecification` ที่มี `minPrice` และ `maxPrice` ราคา 0 ไม่ใส่ราคา หน่วยเป็น `THB` ลิงก์ของแต่ละ `Offer` คือหน้าจองที่เลือกบริการนั้นไว้

JSON-LD ทุกชิ้นผ่าน `serializeJsonLd()` ซึ่งเปลี่ยน `<` `>` `&` เป็นรหัส `<` แบบ JSON ข้อความจาก API เช่นชื่อบทความจึงปิดแท็ก `<script>` เองไม่ได้

## ข้อมูลที่ต้องเติมในระบบ

ข้อมูลโครงสร้างดีเท่าข้อมูลที่คลินิกใส่ไว้ ก่อนเปิดใช้จริงตรวจในหน้าจอพนักงานว่ามีครบ

| ข้อมูล | ใส่ที่ | ถ้าไม่มี |
|---|---|---|
| ชื่อคลินิกและโลโก้ | ตั้งค่า > คลินิกและสาขา | ใช้ชื่อตั้งต้น ไม่มีโลโก้ (ใช้รูปตัวอย่างกลางแทน) |
| ที่อยู่และพิกัดของสาขา | ตั้งค่า > คลินิกและสาขา (ใส่ตอนเปิดสาขา แก้ภายหลังยังไม่ได้ ดู [ตั้งค่าคลินิก](features/clinic-setup.md)) | ไม่มี `address` และ `geo` Google จะไม่แสดงเป็นธุรกิจในพื้นที่ |
| เบอร์โทร อีเมล สายด่วน แผนที่ LINE Facebook | ตั้งค่า > ข้อมูลติดต่อ ดู [ข้อมูลติดต่อ](features/contact.md) | ช่องนั้นไม่มีในผลลัพธ์ |
| เวลาทำการและวันหยุด | ตั้งค่า > เวลาทำการและวันหยุด | ไม่มีเวลาทำการ |

ค่าตัวอย่างจากคำสั่ง `seed-contacts` ไม่ใช่ของจริง ต้องแก้ก่อนเปิดใช้ ดู [หน้าแรกสาธารณะ](features/home-page.md)

## ตรวจว่าทำงาน

**ในเครื่อง** เปิด `npm run dev` แล้ว

1. เปิด `http://localhost:3000/robots.txt` และ `/sitemap.xml` ดูว่าที่อยู่ขึ้นต้นด้วยค่า `NEXT_PUBLIC_SITE_URL`
2. เปิด `/opengraph-image` ต้องเห็นรูป และตัวอักษรไทยไม่เป็นกล่องสี่เหลี่ยม
3. เปิดหน้าใดก็ได้ ดู source (Ctrl+U) หา `og:image` `rel="canonical"` และ `application/ld+json`

**หลังเปิดใช้จริง**

1. [Rich Results Test](https://search.google.com/test/rich-results) ใส่ URL หน้าแรก หน้าติดต่อ หน้าบริการ และบทความหนึ่งเรื่อง ต้องเห็น Local business, Breadcrumbs และ Article โดยไม่มี error ([Schema Markup Validator](https://validator.schema.org/) ใช้ตรวจ `OfferCatalog` ซึ่ง Rich Results Test ไม่แสดง)
2. [Google Search Console](https://search.google.com/search-console) เพิ่มเว็บ (ยืนยันด้วย DNS หรือไฟล์ HTML) แล้วส่ง `https://<โดเมน>/sitemap.xml` ที่เมนู Sitemaps ใช้ URL Inspection ดูว่าหน้าถูกเก็บและ canonical ตรงกับที่ตั้งใจ
3. ดูรูปตัวอย่างลิงก์ด้วย [Facebook Sharing Debugger](https://developers.facebook.com/tools/debug/) (กด Scrape Again หลังแก้) และลองส่งลิงก์ในแชต LINE

## ข้อจำกัด

- หน้าสาธารณะ render ทุกครั้งที่เปิดเพราะอ่าน API แบบไม่ cache ไม่มี ISR
- sitemap มีบทความได้ไม่เกิน 50 เรื่อง เพราะ `GET /articles` ไม่มีการแบ่งหน้า ถ้าบทความเกินนี้ต้องเพิ่มการแบ่งหน้าใน API
- บทความไม่มีวันที่แก้ไขล่าสุดใน API จึงไม่มี `dateModified`
- ยังไม่มีรูปตัวอย่างแยกของแต่ละหน้า ทุกหน้าใช้รูปกลาง ยกเว้นบทความที่มีปกเป็นไฟล์ใต้ `public/`

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| metadata ของหน้า | `frontend/src/features/seo/lib/page-metadata.ts`, `lib/article-metadata.ts` |
| ข้อความและชื่อหน้า | `frontend/src/features/seo/content/seo.ts` |
| JSON-LD | `frontend/src/features/seo/components/` (`json-ld.tsx`, `clinic-json-ld.tsx`, `website-json-ld.tsx`, `breadcrumb-json-ld.tsx`, `article-json-ld.tsx`, `service-catalog-json-ld.tsx`) |
| สร้างข้อมูล schema.org | `frontend/src/features/seo/lib/` (`clinic-schema.ts`, `opening-hours-schema.ts`, `service-catalog-schema.ts`, `json-ld.ts`) |
| รูปตัวอย่าง | `frontend/src/app/(public)/opengraph-image.tsx`, `frontend/src/assets/fonts/` |
| sitemap และ robots | `frontend/src/app/sitemap.ts`, `frontend/src/app/robots.ts` |
| ที่อยู่ของเว็บ | `siteEnv` ใน `frontend/src/config/env.ts`, `metadataBase` ใน `frontend/src/app/layout.tsx` |

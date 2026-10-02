# หน้าแรกสาธารณะ

หน้าแรก (`/`) คือหน้าที่เจ้าของสัตว์เปิดก่อนตัดสินใจมาคลินิก ไม่ต้องเข้าสู่ระบบ ทำตามดีไซน์ที่ได้รับเป็นรูปภาพ เนื้อหาทั้งหมดเป็นข้อความคงที่ในโค้ด **ไม่ได้อ่านจาก API** รูปทุกรูปยังเป็นช่องว่าง และลิงก์หลายตัวยังชี้ `#`

## โครงของหน้า

หน้า `frontend/src/app/(public)/page.tsx` เป็นแค่รายการ section เรียงจากบนลงล่าง ส่วน header และ footer อยู่ใน `app/(public)/layout.tsx` ซึ่งใช้กับทุกหน้าใต้ `(public)`

| ลำดับ | section | โฟลเดอร์ component | ไฟล์เนื้อหา | anchor |
|---|---|---|---|---|
| 1 | Hero หัวเรื่อง ปุ่ม รูป banner | `hero/` | `content/hero.ts` | |
| 2 | ตัวเลขของคลินิก 4 การ์ด | `stats/` | `content/stats.ts` | |
| 3 | บริการ 3 การ์ดใหญ่ 3 การ์ดเล็ก | `services/` | `content/services.ts` | `#services` |
| 4 | คำสัญญาในการดูแล | `care/` | `content/care.ts` | `#about` |
| 5 | ทีมสัตวแพทย์ | `vets/` | อ่านจาก API (`GET /team?department=veterinary`) ดู [ทีมของเรา](team.md) ส่วนหัวข้อและจำนวนคนอยู่ที่ `content/vets.ts` | `#vets` |
| 6 | บทความ และรีวิว (สองคอลัมน์) | `articles/`, `testimonials/` | บทความอ่านจาก API (`GET /articles`) ดู [บทความ](articles.md) ส่วนรีวิวอยู่ที่ `content/testimonials.ts` | `#articles`, `#reviews` |
| 7 | ที่ตั้งและช่องทางติดต่อ | `location/` | `content/location.ts` | `#contact` |
| 8 | ชวนเพิ่มเพื่อน LINE | `line/` | `content/line.ts` | |
| 9 | แถบสีส้มชวนจองนัด | `booking-cta/` | `content/booking-cta.ts` | |

โฟลเดอร์ component อยู่ใต้ `frontend/src/features/home/components/` ไฟล์เนื้อหาอยู่ใต้ `frontend/src/features/home/` ชนิดข้อมูลอยู่ที่ `types/home.types.ts`

component แตกละเอียดตั้งใจ หนึ่งไฟล์คือชิ้นเล็กชิ้นเดียวที่ไม่มี logic นอกจากการวาด ข้อความ ลิงก์ และรูปอยู่ในไฟล์เนื้อหาแยกต่างหาก คนแก้หน้าจึงหาชิ้นที่ต้องการเจอง่าย

## แก้ข้อความ

| อยากแก้ | แก้ที่ |
|---|---|
| ข้อความของ section ใด | `features/home/content/<section>.ts` |
| เมนูบน header ปุ่มเข้าสู่ระบบ ปุ่มสมัคร | `frontend/src/config/site-navigation.ts` |
| ทุกอย่างใน footer (สายด่วน ที่อยู่ ติดต่อ เวลาทำการ ใบอนุญาต ลิงก์ ลิขสิทธิ์) | `frontend/src/config/site-footer.ts` |
| ลำดับ section | ลำดับบรรทัดใน `app/(public)/page.tsx` |
| ชื่อแท็บของเบราว์เซอร์ | `metadata` ใน `frontend/src/app/layout.tsx` |

- รายการที่มีเลขกำกับ (คำสัญญาในการดูแล ข้อดีของ LINE) ใช้ลำดับในไฟล์เป็นเลข ไม่ต้องใส่เลขเอง
- คำรีวิวเขียนโดยไม่ใส่เครื่องหมายคำพูด การ์ดใส่ให้
- ข้อความที่แบ่งเป็น array (`headingLines`, `description`, `quote`) คือการขึ้นบรรทัดบนจอกว้าง หนึ่งช่องต่อหนึ่งบรรทัด
- สีของการ์ดตัวเลขเลือกด้วย `tone` (`green`, `orange`, `purple`, `gray`) ป้ายของบริการใช้ `neutral`, `warning`, `notice`

## เปลี่ยนรูป

ทุกที่ที่ดีไซน์มีรูปใช้ `ImageSlot` ถ้าไม่มี `src` จะวาดกรอบเส้นประพร้อมคำบอกว่ารูปอะไรควรอยู่ตรงนี้ หน้าถูกจัดรอบช่อง ไม่ใช่รอบรูป ใส่รูปภายหลังจึงแก้แค่ `src` โดยไม่มีอะไรขยับ

1. ใส่ไฟล์ใน `frontend/public/images/home/` (ตอนนี้ `public/` ยังว่าง ต้องสร้างโฟลเดอร์เอง)
2. ตั้ง `src: '/images/home/<ชื่อไฟล์>'` ในไฟล์เนื้อหา
3. ตรวจ `alt` ให้บอกว่าในรูปมีอะไร

| ช่อง | ตั้งที่ | หมายเหตุ |
|---|---|---|
| รูป banner (สุนัขและแมว) | `heroImage` ใน `content/hero.ts` | รูปตัดพื้นหลัง วางชิดขอบล่างแบบไม่ครอป (`object-contain`) โหลดก่อนอย่างอื่น (`priority`) |
| รูปข้างคำสัญญา | `careImage` ใน `content/care.ts` | |
| รูปบริการ 3 รูป | `image` ของแต่ละการ์ดใน `content/services.ts` | การ์ดเล็กใช้ไอคอนวาดเองใน `services/icons/` ไม่ใช่รูป |
| รูปหมอ | `photo_path` ในโปรไฟล์สาธารณะของแต่ละคน ดู [ทีมของเรา](team.md) | วางไฟล์ใต้ `frontend/public/images/team/` |
| รูปบทความ | `cover_image_path` ของบทความในฐานข้อมูล ดู [บทความ](articles.md) | วางไฟล์ใต้ `frontend/public/` |
| แผนที่ใน section ที่ตั้ง | `locationMap` ใน `content/location.ts` | ใส่ `embedUrl` (ลิงก์ embed ของ Google Maps จาก Share แล้ว Embed a map เอาค่า `src` ของ iframe) จะแสดงแผนที่สด ถ้าไม่มีใช้ `imageSrc` เป็นรูป |
| แผนที่เล็กใน footer | `footerAddress.map.imageSrc` ใน `config/site-footer.ts` | |

วงกลมสีข้างตัวเลข 9k+ ใน Hero ไม่ใช่รูป เป็นวงกลมสีที่วาดใน `hero/avatar-stack.tsx` ถ้าต้องการรูปลูกค้าจริงต้องแก้ component

## โลโก้ชั่วคราว

- โลโก้บน header และ footer เป็นรูปวาด SVG ชั่วคราว (อุ้งเท้าที่มีคลินิกและกากบาท) ใน `components/layout/site-header/logo-mark.tsx` สีเขียนตายตัวในไฟล์
- เมื่อได้โลโก้จริง ใส่ไฟล์ใน `public/` แล้วแทน `<LogoMark />` ใน `site-logo.tsx` ไฟล์เดียว ไม่มีที่อื่นอ้างถึง `LogoMark`
- ชื่อข้างโลโก้ (Happy Paws Clinic) ใช้ฟอนต์ Inter ตัวหนา อยู่ใน `site-logo.tsx` บน footer ใช้ `tone="light"` เป็นตัวอักษรสีขาว ตัวรูปวาดชั่วคราวมีอุ้งเท้าสีเทาเข้มซึ่งเห็นยากบนพื้น footer สีเข้ม โลโก้จริงต้องมีแบบที่ใช้บนพื้นเข้มได้
- ไอคอนแท็บเบราว์เซอร์ (`frontend/src/app/icon.svg`) เป็นอีกรูปวาดหนึ่ง (อุ้งเท้าขาวบนพื้นเขียว) ต้องเปลี่ยนแยก

## ลิงก์ที่ยังชี้ `#`

ปลายทางเหล่านี้ยังไม่มี เมื่อมีแล้วให้แก้ `href` ในไฟล์ที่บอก

| ลิงก์ | ไฟล์ | ควรไปที่ไหน |
|---|---|---|
| ปุ่มจองนัดหมายใน Hero | `content/hero.ts` (`primaryAction`) | หน้าจองออนไลน์ (ยังไม่มี) |
| ปุ่มจองนัดหมายในแถบสีส้ม | `content/booking-cta.ts` (`primaryAction`) | หน้าจองออนไลน์ |
| เพิ่มเพื่อนใน LINE | `content/line.ts` (`action`) | ลิงก์เพิ่มเพื่อนของคลินิก `https://line.me/R/ti/p/@...` |
| สมัครใช้งานฟรี | `config/site-navigation.ts` (`signUpLink`) | หน้าสมัครของลูกค้า (ยังไม่มี) |
| เปิดใน Google Map | `config/site-footer.ts` (`footerAddress.mapLink`) | ลิงก์ Google Maps ของคลินิก |
| Instagram, LINE, Facebook | `config/site-footer.ts` (`footerContact.socialLinks`) | บัญชีของคลินิก |
| จองนัดหมาย และ ร่วมงานกับเรา ในแถวลิงก์ footer | `config/site-footer.ts` (`footerLinks`) | หน้าจอง และหน้ารับสมัครงาน (ยังไม่มี) |
| Privacy Policy | `config/site-footer.ts` (`copyright.privacyLink`) | ประกาศความเป็นส่วนตัว (ยังไม่มี) |

ปุ่ม **เข้าสู่ระบบ** ทั้งบน header และในแถบสีส้มไปที่ `/login` ซึ่งตอนนี้คือหน้าเข้าสู่ระบบของพนักงาน หน้าเข้าสู่ระบบของลูกค้ายังไม่มี

## ข้อความในดีไซน์ที่ซ้ำหรือขัดกันเอง

ข้อความทั้งหมดคัดจากดีไซน์ตามที่ได้รับ ยังไม่ได้ตรวจกับคลินิก ข้อเหล่านี้เห็นชัดว่าผิดและต้องถามเจ้าของงานก่อนเปิดใช้จริง

| เรื่อง | ที่ไหน | ปัญหา |
|---|---|---|
| เวลาทำการ | `content/stats.ts` การ์ดที่ 4, `content/location.ts` | สองที่นี้บอกว่าเปิดทุกวัน 09:00-20:00 ไม่เว้นวันหยุด แต่ footer (`footerHours`) บอกว่าจันทร์ อังคาร พฤหัสบดี ศุกร์ 09:00-20:00 เสาร์และอาทิตย์ 09:00-17:00 และพุธปิด |
| เวลาทำการกับระบบ | ทั้งหน้า | เวลาจริงของคลินิกตั้งในระบบได้แล้ว (`GET /clinic/hours`) แต่หน้าแรกไม่ได้อ่าน แก้เวลาในระบบแล้วต้องแก้หน้าแรกเองด้วย |
| คำอธิบายการ์ดตัวเลข | `content/stats.ts` | การ์ด 4 ท่าน ใช้คำอธิบายเดียวกับการ์ด 15 ปี (ดูแลสัตว์เล็กในย่านนี้) |
| บริการตรวจสุขภาพ | `content/services.ts` การ์ด `checkup` | คำอธิบายและป้าย งดอาหาร 8 ชม. ซ้ำกับการ์ดทันตกรรม |
| ราคาแล็บ | `content/services.ts` การ์ด `lab` | มีคำว่าเริ่ม ฿800 และเริ่ม ฿1,800 ติดกัน |
| รีวิว | `content/testimonials.ts` | ทั้งสองรีวิวใช้คำพูดเดียวกันทุกคำ |
| ทีมสัตวแพทย์ | `SampleStaff.cs` | ชื่อ ประวัติ และเลขใบอนุญาตของบุคลากรตัวอย่างเป็นของสมมติ การ์ดในดีไซน์มีป้าย "ว่างวันนี้" ซึ่งยังทำจริงไม่ได้ เพราะไม่มีข้อมูลกะทำงาน จึงแสดงตำแหน่งแทน |
| ปุ่มดูบริการ | `content/hero.ts` | ปุ่มเขียนว่า ดูบริการราคา ส่วน footer เขียนว่า บริการและราคา |
| LINE ผูกบัญชี | `content/line.ts` | บอกว่าแอดเพื่อนแล้วระบบผูกกับบัญชีอัตโนมัติ ขัดกับการตัดสินใจของโปรเจกต์ว่า LINE เป็นช่องทางส่งข้อความออกเท่านั้น ไม่ใช่การผูกบัญชี ([assumptions.md](../assumptions.md) หัวข้อ A) |
| สิ่งที่ยังไม่มีจริง | `content/line.ts`, `content/care.ts`, `content/hero.ts`, `content/booking-cta.ts` | เตือนก่อนนัด แจ้งเตือนคิวเมื่อเหลือ 1 คิว เลื่อนหรือยกเลิกนัดในแชต ส่งผลแล็บและเตือนวัคซีนเข้า LINE ดูประวัติสุขภาพออนไลน์ และจองออนไลน์ ยังไม่มีบนเว็บ และการส่งข้อความยังเป็น stub ดู [การแจ้งเตือน](notifications.md) |
| ตัวเลขของคลินิก | `content/hero.ts`, `content/stats.ts` | 9,400 ตัว คะแนน 4.9 จาก 1,240 ครอบครัว 15 ปี 4 ท่าน และรู้ผลเลือดใน 30 นาที เป็นตัวเลขจากดีไซน์ ต้องยืนยันกับคลินิก |
| ข้อมูลสถานพยาบาล | `config/site-footer.ts` (`footerLegal`) | เลขใบอนุญาต ชื่อและเลขใบประกอบวิชาชีพ และเลขนิติบุคคลมาจากดีไซน์ ข้อมูลเหล่านี้กฎหมายบังคับให้แสดง ห้ามเปิดใช้ด้วยค่าตัวอย่าง |
| เบอร์และอีเมล | `config/site-footer.ts`, `content/location.ts` | เบอร์คลินิก สายด่วน อีเมล ที่อยู่ และ LINE ID มาจากดีไซน์ |
| ที่อยู่ | `content/location.ts` กับ `config/site-footer.ts` | เขียนสองแบบ (กรุงเทพฯ กับ กรุงเทพมหานคร และการบอกทางจาก BTS ไม่เหมือนกัน) แก้ให้ตรงกันทั้งสองที่ |
| ลำดับวันใน footer | `config/site-footer.ts` (`footerHours`) | วันพุธอยู่บรรทัดสุดท้าย ไม่เรียงตามวัน (แสดงตามลำดับในไฟล์) |

## ขนาดเมื่อเทียบกับดีไซน์

ดีไซน์ส่งมาเป็นรูปขนาดใหญ่ (กว้าง 1568 และ 1394 px) ทำตามขนาดพิกเซลของดีไซน์แล้วใหญ่เกินไป หน้าจริงจึงย่อลงราว 0.73 ถึง 0.82 เท่า ส่วนสัดส่วน ข้อความ และสียึดตามดีไซน์

- คอลัมน์เนื้อหากว้าง 992 px คือ `SiteContainer` (`max-w-[1040px]` รวม padding ข้างละ 24 px) ใน `components/layout/site-container.tsx` ทุก section และ footer ใช้ตัวนี้ ขอบซ้ายขวาจึงตรงกัน
- header กว้างกว่า (`max-w-[1120px]`)
- หัวเรื่อง Hero สูงสุด 48 px หัวข้อ section 30 px ผ่าน `SectionHeading`
- section ใหม่ให้วัดความกว้างคอลัมน์เนื้อหาในดีไซน์ แล้วคูณทุกระยะด้วย 992 หารด้วยความกว้างนั้น และใช้ `SectionContainer` กับ `SectionHeading` เพื่อให้ได้ขนาดเดียวกัน

## จอเล็ก

- **header** เมนูเต็มแถวแสดงที่ 1280 px ขึ้นไป (`xl`) ต่ำกว่านั้นเป็นปุ่มเมนู ซึ่งเป็น `<details>` เปิดปิดได้โดยไม่ใช้ JavaScript ใช้ได้ก่อนหน้าโหลดเสร็จ ต่ำกว่า 640 px ปุ่มเข้าสู่ระบบและสมัครย้ายเข้าไปในเมนู
- **Hero** จอกว้างรูป banner ยืนบนขอบล่างและล้นขวา จอแคบรูปอยู่ใต้ข้อความ
- **การ์ด** บริการ 1 คอลัมน์ 2 คอลัมน์ (`md`) แล้ว 3 คอลัมน์ (`lg`) หมอ 1, 2 แล้ว 4 คอลัมน์ บทความกับรีวิววางคู่กันที่ `lg`
- **footer** 1 คอลัมน์ 2 คอลัมน์ (`sm`) แล้ว 4 คอลัมน์ (`xl`) ความกว้างสี่คอลัมน์ไม่เท่ากันตามดีไซน์

สี ฟอนต์ และหลักการทั่วไปของหน้าสาธารณะดู [UI/UX](../ui-ux.md)

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| หน้าและ layout | `frontend/src/app/(public)/page.tsx`, `frontend/src/app/(public)/layout.tsx` |
| section | `frontend/src/features/home/components/` |
| เนื้อหา | `frontend/src/features/home/content/` |
| ชนิดข้อมูล | `frontend/src/features/home/types/home.types.ts` |
| header | `frontend/src/components/layout/site-header/`, `frontend/src/config/site-navigation.ts` |
| footer | `frontend/src/components/layout/site-footer/`, `frontend/src/config/site-footer.ts` |
| ช่องรูป | `frontend/src/components/shared/image-slot.tsx` |
| คอลัมน์เนื้อหา | `frontend/src/components/layout/site-container.tsx` |
| ปุ่มของหน้าสาธารณะ | `frontend/src/components/ui/cta-link.tsx` |
| โลโก้ชั่วคราวและไอคอนแท็บ | `frontend/src/components/layout/site-header/logo-mark.tsx`, `site-logo.tsx`, `frontend/src/app/icon.svg` |
| สีของหน้าสาธารณะ | `frontend/src/app/globals.css` (`--color-site-*`, `--color-stat-*`) |

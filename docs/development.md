# วิธีรันและพัฒนา

> **ยังไม่เคยรันระบบนี้ เริ่มที่ [เริ่มต้นใช้งาน](getting-started.md)** หน้านี้เป็นรายละเอียดสำหรับคนที่รันได้แล้ว

เอกสารนี้อธิบายคำสั่ง operator ตัวแปรแวดล้อม การทดสอบ กติกาที่เครื่องมือบังคับ และปัญหาที่เจอบ่อย โครงโฟลเดอร์และวิธีเพิ่ม use case อยู่ใน [โครงสร้างโค้ด](code-structure.md)

## สิ่งที่ต้องมี

| โปรแกรม | รุ่น | ใช้ทำอะไร |
|---|---|---|
| .NET SDK | 10.0 (ทดสอบกับ 10.0.400) | build และรัน API |
| Node.js | 22 (Dockerfile ใช้ `node:22-alpine`) | หน้าเว็บ Next.js |
| Docker Desktop | รุ่นปัจจุบัน | PostgreSQL ของ compose และฐานข้อมูลของ integration test |

## รันทั้งหมดด้วย Docker Compose

ทางนี้เป็นทางเลือก ทางหลักคือรันฐานข้อมูลใน Docker แล้วรัน API กับหน้าเว็บบนเครื่องตาม [เริ่มต้นใช้งาน](getting-started.md) ซึ่งแก้โค้ดแล้วเห็นผลทันที

`docker-compose.yml` มีบริการ `postgres`, `api` (พอร์ต 5080) และ `web` (พอร์ต 3000) คำสั่งคือ

```sh
docker compose up --build
docker compose run --rm api migrate
docker compose run --rm api bootstrap --owner-email owner@example.test --owner-name "คุณเจ้าของ" --clinic-name "Happy Paws Clinic"
```

image ของ `api` build ผ่านแล้ว (`backend/Dockerfile` คัดลอก `Directory.Packages.props` ก่อน `dotnet restore` เพราะรุ่นของทุกแพ็กเกจอยู่ในไฟล์นั้น) ส่วน image ของ `web` ยังไม่ได้ทดสอบ build

## คำสั่ง operator

ไฟล์ `HappyPaws.Api` ตัวเดียวกับที่เปิด API รันคำสั่งเหล่านี้ได้ เลือกด้วยอาร์กิวเมนต์ตัวแรก เมื่อเป็นคำสั่ง web host จะไม่เปิดเลย (`Operator/OperatorCommands.cs`)

| คำสั่ง | ทำอะไร |
|---|---|
| `migrate` | นำ migration ที่ยังไม่ได้ใช้ขึ้นฐานข้อมูล พิมพ์ `Applied N migration(s)` หรือ `Database is up to date.` |
| `bootstrap` | สร้าง role ตั้งต้น 7 ตัว (`clinic_owner`, `clinic_admin`, `veterinarian`, `receptionist`, `vet_assistant`, `cashier`, `customer`) สาขาแรก และเจ้าของคนแรก รันซ้ำได้ สร้างเฉพาะที่ยังไม่มี และไม่สร้างเจ้าของคนที่สองถ้ามี `clinic_owner` ที่ใช้งานอยู่แล้ว |
| `reset-credentials --email <อีเมล>` | ออกรหัสผ่านและความลับ TOTP ใหม่ และเพิกถอนทุกเซสชันของบัญชีนั้น ใช้เมื่อเจ้าของทำแอป authenticator หาย |
| `seed-articles` | ใส่บทความตัวอย่างสี่เรื่อง รันซ้ำได้ เรื่องที่มีอยู่แล้วถูกข้าม ดู [บทความ](features/articles.md) |
| `seed-staff` | ใส่บุคลากรตัวอย่าง 14 คนครบทุกแผนก ต้องรัน `bootstrap` ก่อน บัญชีที่ใส่เข้าสู่ระบบไม่ได้จนกว่าจะ `reset-credentials` รันซ้ำได้ ดู [ทีมของเรา](features/team.md) |
| `seed-treatment-templates` | ใส่เทมเพลตแผนรักษาตัวอย่างหกแบบ รันซ้ำได้ ชื่อที่มีอยู่แล้วถูกข้าม สัญญาไม่มี endpoint สร้างเทมเพลต คำสั่งนี้จึงเป็นทางเดียวตอนนี้ ดู [งานรักษาต่อเนื่อง](features/clinical-records.md) |
| `seed-contacts` | ใส่ช่องทางติดต่อตัวอย่าง 7 รายการ (โทรศัพท์ สายด่วน อีเมล LINE Facebook Instagram แผนที่) ทำเฉพาะเมื่อยังไม่มีรายการเลย ดู [ข้อมูลติดต่อ](features/contact.md) |

ตัวเลือกของ `bootstrap`

| ตัวเลือก | ความหมาย |
|---|---|
| `--owner-email`, `--owner-name` | บัญชีเจ้าของ บังคับเมื่อยังไม่มีเจ้าของ |
| `--owner-password` | รหัสผ่าน ถ้าไม่ส่งจะสุ่มแล้วพิมพ์ให้ |
| `--clinic-name` | ชื่อคลินิกใน `clinic_profile` |
| `--branch-code`, `--branch-name` | สาขาแรก ใช้เฉพาะเมื่อยังไม่มีสาขา ถ้าไม่ส่ง code จะเป็น `main` และ id เป็น `brn_main` |
| `--branch-rooms` | ห้องตรวจ คั่นด้วยจุลภาค เช่น `room_1,room_2` |

ส่งรหัสผ่านผ่านตัวแปร `HAPPYPAWS_OPERATOR_PASSWORD` แทน `--owner-password` หรือ `--password` ได้ เพื่อไม่ให้ค้างใน history ของ shell ตัวเลือกเขียนได้ทั้ง `--name value` และ `--name=value` exit code คือ 0 สำเร็จ 1 ถูกปฏิเสธ 2 ข้อมูลไม่ถูกต้อง

**ทำไมเป็นคำสั่ง ไม่ใช่ endpoint** ทั้งสามอย่างเป็นสิ่งที่ไม่ควรมีทางทำผ่าน HTTP: เปลี่ยน schema, สร้างเจ้าของตอนที่ยังไม่มีใครมีสิทธิ์, เปลี่ยนรหัสผ่านโดยไม่รู้รหัสเดิม และสัญญาก็เขียนว่าไม่มี platform admin คนที่รัน process บนเครื่องได้ถือฐานข้อมูลอยู่แล้ว คำสั่งนี้แค่ให้ทางที่ถูกต้องและบันทึก audit ด้วย

## เมื่อพอร์ต 5432 ถูกใช้

เกิดบ่อยเมื่อเครื่องมี PostgreSQL ติดตั้งอยู่แล้ว

1. ตั้ง `POSTGRES_PORT=5433` ใน `.env` แล้ว `docker compose up -d postgres` ใหม่ ค่านี้เปลี่ยนแค่พอร์ตฝั่งเครื่อง บริการ `api` ใน compose ยังต่อ `postgres:5432` ผ่านเครือข่ายของ compose
2. API ที่รันบนเครื่องยังชี้ `localhost:5432` ตาม `appsettings.Development.json` ให้ตั้ง connection string ใหม่ เลือกทางใดทางหนึ่ง

```sh
# ตัวแปรแวดล้อม (bash)
export ConnectionStrings__Postgres="Host=localhost;Port=5433;Database=happypaws;Username=happypaws;Password=happypaws"

# หรือ user-secrets ของโปรเจกต์ API (UserSecretsId = happypaws-api)
dotnet user-secrets --project src/HappyPaws.Api set "ConnectionStrings:Postgres" "Host=localhost;Port=5433;Database=happypaws;Username=happypaws;Password=happypaws"
```

บน PowerShell ใช้ `$env:ConnectionStrings__Postgres = "..."` แทน `export` ตั้งแล้วรัน `migrate`, `bootstrap` และ API ใน Terminal เดียวกัน

## path `/v1`

URL ฐานของสัญญาคือ `https://api.happypaws.co.th/v1` API จึงใช้ `UsePathBase("/v1")` (`Http/ApiVersion.cs`)

- `/v1/clinic` คือที่อยู่ที่ควรใช้ ส่วน `/clinic` ก็ยังตอบได้ เพราะ `UsePathBase` ปล่อย path ที่ไม่มี prefix ผ่านไปตรง ๆ มีไว้สำหรับ reverse proxy ที่ตัด `/v1` ออกให้แล้ว
- `API_BASE_URL` ของหน้าเว็บต้องลงท้ายด้วย `/v1` ตาม `frontend/.env.example` คุกกี้ `hp_refresh` ของ API ตั้ง path ตามที่ผู้เรียกใช้ (`/v1/auth`)
- path ที่ไม่มีอยู่จริงตอบ **401 ไม่ใช่ 404** ถ้าไม่ได้ส่ง token เพราะ `FallbackPolicy` บังคับเข้าสู่ระบบก่อนจะรู้ว่าไม่มี route เช่น `http://localhost:5080/` และ `/openapi/v1.json` ได้ 401

## ตัวแปรแวดล้อม

**ฝั่ง API** ค่าใน `appsettings.json` เขียนทับได้ด้วยตัวแปรที่ใช้ `__` แทน `:`

| ตัวแปร | ค่าใน Development | ความหมาย |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` (launch profile และ compose) | เลือก `appsettings.Development.json` |
| `ConnectionStrings__Postgres` | `Host=localhost;Port=5432;...` | ฐานข้อมูล |
| `Auth__Jwt__Issuer` | `http://localhost:5080` | บังคับ ไม่มีแล้ว API ไม่เปิด |
| `Auth__Jwt__SigningKey` | คีย์ dev ใน `appsettings.Development.json` | บังคับ ยาวอย่างน้อย 32 ตัวอักษร ห้ามใช้คีย์ dev นอกเครื่องตัวเอง |
| `Auth__Jwt__AccessTokenLifetimeMinutes` | 15 | อายุ access token (1 ถึง 60) |
| `Cors__AllowedOrigins__0` | `http://localhost:3000` | origin ที่อนุญาต |
| `Staff__Invitations__LogUndeliveredLinks` | `true` | เขียนลิงก์คำเชิญพนักงานลง log เมื่อยังไม่ได้ตั้งอีเมล ลิงก์นี้เป็นความลับ |
| `Staff__Invitations__AcceptUrlTemplate` | `http://localhost:3000/invitations/{id}#token={token}` | หน้าเว็บที่ลิงก์คำเชิญชี้ไป ต้องเปลี่ยนเป็นโดเมนจริงตอนติดตั้ง |
| `Auth__SignIn__LogUndeliveredSecrets` | `true` | เขียนรหัส OTP ของลูกค้าและลิงก์รีเซ็ตรหัสผ่านลง log เมื่อยังไม่ได้ตั้ง SMS หรืออีเมล ทั้งสองอย่างเป็นความลับ |
| `Messaging__Email__Host` | ว่าง | SMTP server เช่น `smtp.gmail.com` ว่างคือไม่ส่งอีเมล ดูหัวข้อ [ตั้งค่าอีเมลและ SMS](#ตั้งค่าอีเมลและ-sms) |
| `Messaging__Email__Port`, `Security` | `587`, `starttls` | `ssl` สำหรับพอร์ต 465 |
| `Messaging__Email__Username`, `Password` | ว่าง | บัญชี SMTP ตั้ง `Password` ด้วยตัวแปรแวดล้อมเท่านั้น |
| `Messaging__Email__FromAddress`, `FromName` | ว่าง | ผู้ส่ง `FromName` ว่างคือใช้ชื่อคลินิก |
| `Messaging__Sms__Provider` | ว่าง | `thaibulksms` คือส่ง SMS ว่างคือไม่ส่ง |
| `Messaging__Sms__ApiKey`, `ApiSecret` | ว่าง | กุญแจของ ThaiBulkSMS ตั้ง `ApiSecret` ด้วยตัวแปรแวดล้อมเท่านั้น |
| `Messaging__Sms__Sender` | ว่าง | ชื่อผู้ส่งที่ลงทะเบียนกับ ThaiBulkSMS ว่างคือชื่อตั้งต้นของบัญชี |
| `Messaging__Line__ChannelId` | ว่าง | Channel ID ของ LINE Official Account ดูหัวข้อ [ตั้งค่า LINE](#ตั้งค่า-line) |
| `Messaging__Line__ChannelSecret` | ว่าง | ใช้ตรวจลายเซ็น webhook ตั้งด้วยตัวแปรแวดล้อมเท่านั้น ว่างและไม่ได้ตั้งในหน้าตั้งค่า คือ webhook ตอบ 401 ทุกครั้ง |
| `Messaging__Line__ChannelAccessToken` | ว่าง | token แบบ long-lived สำหรับ push ตั้งด้วยตัวแปรแวดล้อมเท่านั้น ว่างและไม่ได้ตั้งในหน้าตั้งค่า คือไม่ส่ง LINE |
| `Messaging__Line__BaseUrl` | `https://api.line.me` | เปลี่ยนเฉพาะตอนทดสอบกับเซิร์ฟเวอร์จำลอง |
| `Messaging__Line__PortalUrl` | `http://localhost:3000/portal` | ลิงก์ portal ที่ข้อความ LINE และคำตอบในแชตใช้ |
| `Auth__SignIn__PasswordResetUrlTemplate` | `http://localhost:3000/reset-password#token={token}` | หน้าเว็บที่ลิงก์รีเซ็ตรหัสผ่านชี้ไป |
| `RateLimits__OtpPerPhonePer10Minutes` | 3 | จำนวนรหัส OTP ที่ขอได้ต่อเบอร์ใน 10 นาที |
| `Storage__LocalRoot` | ว่าง (โฟลเดอร์ชั่วคราวของระบบ) | ที่เก็บไฟล์ที่อัปโหลด ต้องตั้งเป็นโฟลเดอร์ถาวรก่อนใช้งานจริง |
| `Storage__SigningKey` | ว่าง (คำนวณจากคีย์ JWT) | คีย์ลงชื่อ URL อัปโหลดและดาวน์โหลด ถ้าตั้งเองต้องยาวอย่างน้อย 32 ตัวอักษร |
| `Storage__PublicBaseUrl` | `http://localhost:5080/v1` | ส่วนต้นของ URL อัปโหลดและดาวน์โหลดที่ API ออกให้ |
| `Jobs__Enabled` | `true` | ตัวตั้งเวลาเบื้องหลังทั้งหมดของ instance นี้ `false` คือปิดทุกงาน ดูหัวข้อ [ตัวตั้งเวลาเบื้องหลัง](#ตัวตั้งเวลาเบื้องหลัง) |
| `Jobs__StartupDelaySeconds` | 10 | รอกี่วินาทีหลังเปิด API ก่อนรันงานแรก |
| `Jobs__Automations__Enabled`, `IntervalSeconds` | `true`, 60 | งานเตือนอัตโนมัติ และรอบของมัน |
| `Jobs__Automations__CatchUpHours` | 12 | ข้อความที่เลยเวลาส่งไปแล้วยังส่งตามหลังได้กี่ชั่วโมง (1 ถึง 72) |
| `Jobs__ReportExports__Enabled`, `IntervalSeconds` | `true`, 15 | งานสร้างไฟล์ส่งออกรายงาน |
| `Jobs__Housekeeping__Enabled`, `IntervalSeconds` | `true`, 3600 | งานลบ hold และ idempotency key ที่หมดอายุเกินหนึ่งวัน |
| `Jobs__Webhooks__Enabled`, `IntervalSeconds` | `true`, 10 | งานแตก outbox เป็น delivery และส่ง webhook |
| `Webhooks__TimeoutSeconds` | 10 | เวลาสูงสุดของการส่ง webhook หนึ่งครั้ง (1 ถึง 30) |
| `Webhooks__AllowLocalhostInDevelopment` | `true` | ให้ webhook และ URL ของแล็บเป็น `http://localhost` ได้ใน Development เท่านั้น นอก Development ไม่มีผล ช่วงส่วนตัวและ metadata ถูกปฏิเสธเสมอ |
| `RateLimits__PerApiKeyPerMinute` | เท่ากับ `PerTokenPerMinute` (600) | จำนวนคำขอต่อนาทีของ API key หนึ่งอัน นับแยกจากผู้สร้าง |
| `DataProtection__KeysDirectory` | ว่าง (ค่าเริ่มต้นของ ASP.NET Core) | โฟลเดอร์เก็บกุญแจที่เข้ารหัส secret ของ webhook ค่าความลับของผู้ให้บริการ และ token ของลิงก์ปฏิทิน ดูหัวข้อ [กุญแจเข้ารหัสความลับ](#กุญแจเข้ารหัสความลับ) |
| `HAPPYPAWS_OPERATOR_PASSWORD` | ว่าง | รหัสผ่านสำหรับ `bootstrap` และ `reset-credentials` |
| `HAPPYPAWS_DESIGNTIME_CONNECTION` | ว่าง | ฐานข้อมูลของ `dotnet-ef` ดู [ฐานข้อมูล](database.md#migration) |

ตัวแปร `LINE_CHANNEL_ID`, `LINE_CHANNEL_SECRET`, `LINE_CHANNEL_ACCESS_TOKEN` ใน `.env.example` ถูกส่งต่อเป็น `Messaging__Line__*` ใน `docker-compose.yml`

**ฝั่งหน้าเว็บ** เขียนใน `frontend/.env.local`

| ตัวแปร | ค่าตัวอย่าง | ความหมาย |
|---|---|---|
| `API_BASE_URL` | `http://localhost:5080/v1` | ที่อยู่ API ที่เซิร์ฟเวอร์ Next.js เรียก ไม่มีแล้วทุกคำขอที่เรียก API ล้ม |
| `NEXT_PUBLIC_API_BASE_URL` | `http://localhost:5080/v1` | ที่อยู่ API ที่คนนอกเข้าถึงได้ หน้าลิงก์ปฏิทินใช้ประกอบ URL ที่แอปปฏิทินดึง ต้องเป็นที่อยู่สาธารณะตอนติดตั้งจริง |
| `NEXT_PUBLIC_SITE_URL` | `http://localhost:3000` | ที่อยู่ของเว็บสาธารณะ ไม่มี `/` ท้าย ใช้สร้างลิงก์ canonical รูปตัวอย่างเวลาแชร์ลิงก์ sitemap และข้อมูลโครงสร้าง ถ้าไม่ตั้งจะใช้ `http://localhost:3000` ค่านี้ถูกฝังตอน build ต้องตั้งก่อน `next build` (Docker ส่งเป็น build arg จาก `.env`) ดู [SEO](seo.md) |

## ตั้งค่าอีเมลและ SMS

ข้อความเหล่านี้ส่งจริงเมื่อตั้งค่านี้ ถ้าไม่ตั้ง ระบบเขียนลง log แทนเหมือนเดิม

| ข้อความ | ช่องทาง |
|---|---|
| รหัส OTP ตอนลูกค้าสมัครหรือเข้าสู่ระบบ | SMS |
| ลิงก์ตั้งรหัสผ่านใหม่ | อีเมลถ้าบัญชีมีอีเมลและตั้งอีเมลไว้ ไม่อย่างนั้น SMS |
| ลิงก์คำเชิญพนักงาน | อีเมล |
| การแจ้งเตือนถึงลูกค้า (เรียกคิว ใบเสร็จ เตือนนัด ข้อความจากพนักงาน) | LINE ก่อน แล้ว SMS แล้วอีเมล ตามที่ตั้งไว้ ดู [การแจ้งเตือน](features/notifications.md) |

ถ้าไม่ได้ตั้ง LINE, SMS และอีเมลเลย การแจ้งเตือนถึงลูกค้าถูกเขียนลง log ระดับ warning และประวัติการส่ง (`GET /staff/notifications/log`) บันทึกว่า `channel_not_configured`

**อีเมล** ใช้ SMTP ของผู้ให้บริการรายไหนก็ได้ ตัวอย่างกับ Google Workspace (ต้องสร้าง App Password ในบัญชี Google ก่อน)

```powershell
$env:Messaging__Email__Host = "smtp.gmail.com"
$env:Messaging__Email__Username = "no-reply@yourclinic.com"
$env:Messaging__Email__Password = "<app password>"
$env:Messaging__Email__FromAddress = "no-reply@yourclinic.com"
```

Amazon SES, Resend และ SendGrid ใช้ค่าชุดเดียวกัน เปลี่ยนแค่ Host และบัญชีตามที่ผู้ให้บริการบอก

**SMS** ใช้ ThaiBulkSMS สมัครบัญชีแล้วสร้าง API key และ secret ที่หน้าตั้งค่าของเว็บ ThaiBulkSMS

```powershell
$env:Messaging__Sms__Provider = "thaibulksms"
$env:Messaging__Sms__ApiKey = "<api key>"
$env:Messaging__Sms__ApiSecret = "<api secret>"
$env:Messaging__Sms__Sender = "<ชื่อผู้ส่งที่ลงทะเบียนไว้>"
```

- ตั้งรหัสผ่านและ secret ด้วยตัวแปรแวดล้อมเท่านั้น ห้ามใส่ใน `appsettings.json` ที่ขึ้น git
- ถ้าส่งไม่สำเร็จ เช่น เครดิตหมดหรือรหัสผ่าน SMTP ผิด คำขอของผู้ใช้ยังสำเร็จ แต่ log ของ API มีบรรทัดระดับ error ว่า `not delivered` พร้อมเหตุผล ไม่มีรหัสหรือลิงก์อยู่ในบรรทัดนั้น
- ลิงก์ในข้อความใช้ `Auth__SignIn__PasswordResetUrlTemplate` และ `Staff__Invitations__AcceptUrlTemplate` ต้องเปลี่ยนเป็นโดเมนจริงของคลินิกก่อนใช้งาน
- เมื่อตั้งค่าแล้ว ควรปิด `Auth__SignIn__LogUndeliveredSecrets` และ `Staff__Invitations__LogUndeliveredLinks` ในเครื่องที่ใช้งานจริง
- รูปแบบคำขอไปที่ ThaiBulkSMS เขียนตาม API v2 (`POST /sms` แบบ basic auth) ทดสอบแล้วกับเซิร์ฟเวอร์จำลองเท่านั้น ควรลองส่งจริงด้วยบัญชีของคลินิกหนึ่งครั้งก่อนเปิดใช้

## ตั้งค่า LINE

1. สร้างช่อง Messaging API ของ LINE Official Account คลินิกที่ LINE Developers
2. คัด Channel ID และ Channel secret จากหน้า Basic settings ออก channel access token แบบ long-lived ที่หน้า Messaging API
3. ตั้งค่าบนเครื่องที่รัน API ด้วยตัวแปรแวดล้อม (หรือใส่ในหน้า "ตั้งค่า > LINE" ของระบบ ซึ่งเก็บในฐานข้อมูลแบบยังไม่เข้ารหัส)

```powershell
$env:Messaging__Line__ChannelId = "<channel id>"
$env:Messaging__Line__ChannelSecret = "<channel secret>"
$env:Messaging__Line__ChannelAccessToken = "<channel access token>"
$env:Messaging__Line__PortalUrl = "https://yourclinic.com/portal"
```

ถ้ารันด้วย Docker Compose ให้ใส่ `LINE_CHANNEL_ID`, `LINE_CHANNEL_SECRET`, `LINE_CHANNEL_ACCESS_TOKEN` ใน `.env`

4. ที่หน้า Messaging API ของ LINE Developers ใส่ Webhook URL เป็น URL สาธารณะของ API ต่อด้วย `/webhooks/line` เช่น `https://api.yourclinic.com/v1/webhooks/line` เปิด "Use webhook" แล้วกด Verify ต้องได้ 200 (ถ้าได้ 401 คือ secret ไม่ตรง)
5. ปิด "Auto-reply messages" ของ LINE Official Account Manager เพราะระบบตอบข้อความเอง

- ค่าที่ตั้งในหน้าตั้งค่าของระบบชนะค่าจากตัวแปรแวดล้อม ลบค่าในหน้านั้นเพื่อกลับไปใช้ตัวแปรแวดล้อม
- ลูกค้าจะได้ข้อความทาง LINE เมื่อเรกคอร์ดมี LINE user id ให้ลูกค้าเพิ่มเพื่อน LINE ของคลินิก แล้วเคาน์เตอร์เลือกชื่อเขาจากการ์ด LINE ในหน้าลูกค้า ถ้ามี access token ระบบอ่านชื่อและรูปจาก LINE ให้ด้วย ดู [LINE](features/line.md#ที่อยู่-line-ของลูกค้า)
- client ของ LINE ทดสอบกับเซิร์ฟเวอร์จำลองเท่านั้น ควรลองส่งจริงหนึ่งครั้งก่อนเปิดใช้

## ตัวตั้งเวลาเบื้องหลัง

API มีงานเบื้องหลังสี่งาน แต่ละงานเป็น `BackgroundService` ใน `backend/src/HappyPaws.Infrastructure/Jobs/` เปิดเองเมื่อรัน API ไม่ทำงานตอนรันคำสั่ง operator

| งาน | รอบ | ทำอะไร |
|---|---|---|
| `automations` | 60 วินาที | รันกติกาเตือนอัตโนมัติที่เปิดอยู่ ดู [เตือนอัตโนมัติ](features/automation.md) |
| `report-exports` | 15 วินาที | สร้างไฟล์ที่สั่งจาก `POST /staff/reports/exports` |
| `housekeeping` | 1 ชั่วโมง | ลบ booking hold ที่หมดอายุและไม่ได้กลายเป็นนัด และ idempotency key ที่หมดอายุ เมื่อเลยมาแล้วหนึ่งวัน และ event ของ webhook พร้อมประวัติการส่งที่เก่ากว่า 30 วัน |
| `webhooks` | 10 วินาที | แตก event ใน outbox เป็น delivery แล้วส่งที่ถึงเวลา ดู [การเชื่อมต่อระบบ](features/integrations.md) |

**ปิดบนเครื่องตัวเอง** ตั้งตัวแปรก่อนรัน API ใน Terminal เดียวกัน

```powershell
$env:Jobs__Enabled = "false"                 # ปิดทุกงาน
$env:Jobs__Automations__Enabled = "false"    # ปิดเฉพาะการเตือน
```

บน bash ใช้ `export Jobs__Enabled=false` หรือใส่ใน user-secrets (`dotnet user-secrets --project src/HappyPaws.Api set "Jobs:Enabled" "false"`) ตอน API เปิด log บอกว่างานไหนเปิดอยู่ (`Background job automations runs every 00:01:00`) หรือปิดอยู่

- กติกาเริ่มต้นทั้งสี่ข้อปิดไว้ เปิดตัวตั้งเวลาไว้บนเครื่องตัวเองจึงไม่ส่งข้อความถึงใครจนกว่าจะสร้างกติกาที่เปิดใช้ และถ้าไม่ได้ตั้ง LINE, SMS หรืออีเมล ข้อความก็ลงแค่ log
- ถ้าปิดงาน `report-exports` งานส่งออกจะค้างที่ `queued`
- ถ้าปิดงาน `webhooks` event จะค้างใน outbox และส่งเมื่อเปิดอีกครั้ง
- เปิดหลาย instance พร้อมกันได้ ข้อความเตือนจองด้วย unique key งานส่งออกและ webhook จองด้วย `FOR UPDATE SKIP LOCKED` จึงไม่ทำซ้ำ
- ชุด integration test ตั้ง `Jobs:Enabled = false` เสมอ (`Fixtures/ApiFixture.cs`) เทสต์เรียก `RunAutomationsHandler`, `RunReportExportsHandler`, `DispatchWebhooksHandler` และ `HousekeepingJob` เอง

## กุญแจเข้ารหัสความลับ

secret ของ webhook ค่าความลับของผู้ให้บริการชำระเงินและแล็บ และ token ของลิงก์ปฏิทิน ต้องอ่านกลับได้ (ใช้ลงนามหรือเรียกผู้ให้บริการ) จึงเก็บเป็น hash ไม่ได้ ระบบเข้ารหัสด้วย ASP.NET Core Data Protection (`Infrastructure/Webhooks/DataProtectionSecretProtector.cs`) กุญแจอยู่นอกฐานข้อมูล สำเนาฐานข้อมูลอย่างเดียวจึงถอดไม่ได้

| เรื่อง | กติกา |
|---|---|
| ที่เก็บกุญแจ | `DataProtection__KeysDirectory` ถ้าไม่ตั้ง ใช้ค่าเริ่มต้น บน Windows คือ `%LOCALAPPDATA%\ASP.NET\DataProtection-Keys` ใน container คือโฟลเดอร์ใน container ซึ่งหายเมื่อสร้าง container ใหม่ |
| ติดตั้งจริง | ตั้งเป็นโฟลเดอร์ถาวร (volume) ที่สำรองแยกจากฐานข้อมูล และจำกัดสิทธิ์ให้ process ของ API อ่านเขียนได้คนเดียว หลาย instance ต้องชี้ไปที่โฟลเดอร์เดียวกัน (ชื่อแอปตั้งไว้เป็น `HappyPaws.Api` แล้ว) |
| หมุนกุญแจ | กุญแจใหม่ถูกสร้างเองทุก 90 วัน กุญแจเก่าเก็บไว้ถอดค่าที่เข้ารหัสไว้ก่อน อย่าลบไฟล์กุญแจเก่า |
| กุญแจหาย | ค่าที่เข้ารหัสไว้อ่านไม่ได้อีก webhook ส่งไม่สำเร็จด้วย `secret_unavailable` ต้องสร้าง webhook ใหม่ และตั้งความลับของผู้ให้บริการใหม่ ลิงก์ปฏิทินยังใช้ได้ (ค้นด้วย hash) แต่หน้าเว็บแสดงลิงก์เดิมไม่ได้ |
| เทสต์ | `IntegrationsFixture` ตั้งโฟลเดอร์ชั่วคราวของตัวเอง |

ความลับ TOTP และความลับของ LINE ยังเก็บแบบไม่เข้ารหัส เป็นงานที่รู้อยู่และแยกจากนี้ ([system-overview.md § 5 A](./system-overview.md))

## คำสั่งประจำวัน

```sh
# backend/
dotnet build HappyPaws.slnx                    # warning เป็น error
dotnet test HappyPaws.slnx                     # ทุกเทสต์ ต้องเปิด Docker
dotnet test tests/HappyPaws.UnitTests          # เร็ว ไม่ต้องมี Docker
dotnet tool restore                            # ครั้งแรก เพื่อใช้ dotnet dotnet-ef

# frontend/
npm run dev
npm run lint
npm run typecheck
npm run build
```

## การทดสอบ

| โปรเจกต์ | ครอบอะไร | จำนวน |
|---|---|---|
| `tests/HappyPaws.UnitTests` | state machine ของนัดและคิว, ใบแจ้งหนี้, การตรวจ, เวชระเบียน, การรวมลูกค้า, scope, การคำนวณช่องนัด, cursor, `ConcurrencyGuard`, ประตูความยินยอม, แผนกและโปรไฟล์สาธารณะ, RRULE ของกะ การตัดกะด้วยวันลา กติกาจ่ายเคส, โปรไฟล์ตัวเองและ recovery code, วัคซีน ธงผลแล็บ ยาซ้ำซ้อน, ไม่มาตามนัด นโยบายยกเลิก ประเภทนัด คิวรอ, รหัส OTP, โควตารายวันของผู้ช่วย AI, ช่วงเวลาและ cohort ของรายงาน, ถ้อยคำและความยาว SMS ของเทมเพลตแจ้งเตือน, การตั้งค่า LINE และข้อมูลปุ่มในแชต, การปิดบังที่อยู่ในประวัติการส่ง, กติกาเตือนอัตโนมัติและเวลาส่งตามเวลาคลินิก ผลของข้อความเตือนจากประวัติการส่ง สถานะงานส่งออก และไฟล์ CSV, ล็อตและสมุดสต๊อก การหยิบล็อตที่หมดอายุก่อน การรับของตามใบสั่งซื้อ การผูกชื่อยากับรายการในคลัง และแจ้งเตือนสต๊อก, การแก้และลบกติกาเตือน, ผู้ติดตาม LINE, กติกาที่อยู่กัน SSRF และรูปแบบ URL ของ webhook ตารางลองใหม่ ลายเซ็น HMAC API key และ scope ที่ key ถือได้ ไฟล์ iCalendar | 564 |
| `tests/HappyPaws.IntegrationTests` | ยิง HTTP เข้า API จริงผ่าน `WebApplicationFactory` กับ PostgreSQL 17 ใน Testcontainers | 923 |

integration test เป็นที่ตรวจสัญญา: status code, รูปของ error, cursor pagination, `If-Match` ที่ชน, `Idempotency-Key` และที่สำคัญที่สุดคือทุก endpoint ต้องปฏิเสธผู้เรียกที่ไม่มี scope ฝั่งหน้าเว็บยังไม่มีชุดทดสอบ หน้าคิวและหน้าเก็บเงินตรวจด้วยการใช้งานจริงในเบราว์เซอร์กับ API และฐานข้อมูลจริง แต่ไม่ได้บันทึกเป็นสคริปต์

### กลุ่มของ integration test

`Fixtures/ApiFixture.cs` เปิด container PostgreSQL ใหม่ รัน migration แล้วเปิด API ในโหมด Development พร้อม `ValidateOnBuild` ทำให้ DI ที่ต่อผิดล้มตั้งแต่เริ่ม

| กลุ่ม | ใช้กับ | วิธีแยกข้อมูล |
|---|---|---|
| `ApiCollection` | เทสต์เกือบทั้งหมด (62 คลาส รวมเทสต์สต๊อกและการจ่ายยา) | ใช้ฐานข้อมูลร่วมกัน แต่ละเทสต์สร้างข้อมูลของตัวเอง เทสต์ที่ต้องแก้ค่าระดับคลินิก (ประเภทนัด นโยบายยกเลิก ส่วนของเวชระเบียนที่เจ้าของเห็น) คืนค่าเดิมใน `finally` เพราะใน collection เดียวกันเทสต์รันทีละตัว |
| `FreshInstanceCollection` | เทสต์ที่แก้ของที่มีชิ้นเดียวทั้ง instance (3 คลาส: `BootstrapTests`, `ClinicProfileAndBranchTests`, `ClinicSettingsTests`) | container แยกของตัวเอง สืบจาก `FreshInstanceTest` ซึ่งรัน `bootstrap` ก่อนเทสต์แรก |
| `DeliveryCollection` | เทสต์ที่ให้ข้อความออกจริงถึงผู้ให้บริการจำลอง (`DeliveryTests`, `BroadcastTests`, `LineIntegrationTests`, `LineWebhookTests`) | container แยก ใช้ `DeliveryFixture` ซึ่งคืน `DeliveringNotificationChannel` ตัวจริง แทน transport SMS และอีเมลด้วย `FakeTransports` และชี้ HTTP client ของ LINE ไปที่ `FakeLineServer` แต่ละเทสต์ตั้ง LINE และสวิตช์ของตัวเองใน `InitializeAsync` เพราะการตั้งค่า LINE มีแถวเดียวต่อคลินิก |
| `JobsCollection` | งานเบื้องหลัง: เตือนอัตโนมัติ ส่งออกรายงาน และ housekeeping (`AutomationEndpointTests`, `AutomationRunTests`, `HousekeepingTests`, `ReportExportTests`) | container แยก เพราะการรันจริงประเมินทุกกติกาที่เปิดอยู่กับทุกเรกคอร์ด และ worker ส่งออกหยิบทุกงานในคิว ถ้าอยู่ในฐานข้อมูลร่วมจะส่งข้อความถึงลูกค้าของเทสต์อื่น แต่ละเทสต์ยังสร้างสาขาของตัวเองและผูกกติกากับสาขานั้น `AutomationScenario` ช่วยสร้างนัด วัคซีน แผน และรันกติกา ณ เวลาที่เทสต์เลือก |

| `IntegrationsCollection` | webhook API key ค่าตั้งผู้ให้บริการ และฟีดปฏิทิน (`WebhookSubscriptionTests`, `WebhookDeliveryTests`, `WebhookOutboxTests`, `ApiKeyTests`, `ProviderSettingsTests`, `CalendarFeedTests`) | container แยก ใช้ `IntegrationsFixture` ซึ่งชี้ HTTP client ของ webhook ไปที่ `FakeWebhookReceiver` ให้ชื่อโฮสต์ resolve ตามที่เทสต์กำหนด (`ScriptedResolver`) ใช้กติกาที่อยู่แบบ production (ไม่มี localhost) timeout หนึ่งวินาที rate limit ของ API key ห้าครั้งต่อนาที และโฟลเดอร์กุญแจ Data Protection ชั่วคราว แต่ละเทสต์ใช้โฮสต์ปลายทางของตัวเอง เพราะ webhook ทุกตัวได้ event ทุกตัวที่ตรงชนิด |

ใช้ fixture class เดียวกันทั้งสองกลุ่ม xUnit สร้าง fixture หนึ่งตัวต่อ collection การประกาศ collection ที่สองจึงได้ instance ที่สองที่ไม่เห็นข้อมูลของกันเลย ถ้าเทสต์ไหนแก้เวลาทำการ การตั้งค่าคิว การตั้งค่าเวชระเบียน หรือเรื่อง "มีเจ้าของหรือยัง" ต้องอยู่ใน `FreshInstanceCollection` ไม่อย่างนั้นจะเปลี่ยนสภาพใต้เทสต์อีกสองร้อยกว่าตัว

### ข้อตกลงที่ควรรู้ก่อนเพิ่มเทสต์

- **แทนที่เฉพาะ adapter ชั้นนอกสุด** `RecordingNotificationChannel` แทน `INotificationChannel` `RecordingStaffInvitationSender` แทนตัวส่งคำเชิญ และ `RecordingSignInSenders` แทนตัวส่งรหัส OTP และลิงก์รีเซ็ตรหัสผ่าน (อ่านรหัสด้วย `fixture.SignIn.CodeFor(phone)`) ส่วนประตูความยินยอมยังเป็นของจริง ถ้าแทน dispatcher เทสต์เรื่องการระงับข้อความจะผ่านกับระบบที่ไม่ได้ระงับอะไรเลย
- **ระบุวันตามเวลาคลินิกเสมอ** คลินิกอยู่ `Asia/Bangkok` วันที่ UTC กับวันที่คลินิกต่างกัน 7 ชั่วโมงต่อวัน ใช้ `ClinicSeed.ClinicDate(...)` เทสต์จองที่สมมติว่า "วันนี้ยังมีช่องว่าง" จะล้มหลังราว 22:00 อย่างถูกต้อง เพราะต้องจองล่วงหน้า 120 นาที
- **ตรวจ status ของทุกคำขอที่ใช้เตรียมข้อมูล** helper ที่ POST แล้วไม่ตรวจผล ทำให้ assertion ถัดไปล้มห่างจากจุดที่ผิดหลายขั้น ตัวช่วยใน `Fixtures/` เช่น `BookingScenario` ใส่ `Idempotency-Key` ให้แล้ว
- **เข้าสู่ระบบแบบที่เบราว์เซอร์ทำ** `FreshInstance.SignInAsync` เรียก login แล้ว 2FA ด้วยรหัส TOTP จริง ส่วน `TestTokens` ออก token ตรงสำหรับเทสต์ที่ไม่ได้ทดสอบการเข้าสู่ระบบ
- `Probes/ProbeController.cs` เป็น endpoint เฉพาะในเทสต์ ใช้ทดสอบ middleware ข้ามโมดูล ไม่ถูก ship

## กติกาที่บังคับด้วยเครื่องมือ

| กติกา | บังคับโดย |
|---|---|
| warning เป็น error รวมถึงคำเตือนช่องโหว่ของ NuGet | `TreatWarningsAsErrors` ใน `backend/Directory.Build.props` |
| ทุกโปรเจกต์ใช้แพ็กเกจรุ่นเดียวกัน | `ManagePackageVersionsCentrally` และ `CentralPackageTransitivePinningEnabled` ใน `Directory.Packages.props` |
| Domain ไม่พึ่งแพ็กเกจภายนอก | `HappyPaws.Domain.csproj` ไม่มี `PackageReference` |
| ตัวสร้าง PDF อยู่ใน Infrastructure เท่านั้น | QuestPDF อ้างอิงเฉพาะใน `HappyPaws.Infrastructure.csproj` ฟอนต์ Sarabun ฝังเป็น resource ของโปรเจกต์นั้น |
| ตัวเขียน Excel อยู่ใน Infrastructure เท่านั้น | `DocumentFormat.OpenXml` (Open XML SDK ของ Microsoft สัญญาอนุญาต MIT พาแค่ `DocumentFormat.OpenXml.Framework` และ `System.IO.Packaging` ซึ่ง MIT ทั้งคู่) อ้างอิงเฉพาะใน `HappyPaws.Infrastructure.csproj` ใช้กับไฟล์ส่งออก `xlsx` |
| XML doc ที่เขียนต้องถูกรูปแบบ เช่น `cref` ที่ชี้ไปของที่ไม่มีเป็น error แต่ไม่บังคับให้มีทุก member | `GenerateDocumentationFile` โดยปิด CS1591 |
| ตัวตรวจ code style รันตอน build | `EnforceCodeStyleInBuild` ยังไม่มี `.editorconfig` จึงมีแค่กฎตั้งต้นของ SDK |
| ห้าม `fetch` นอก `src/lib/api` | ESLint `no-restricted-globals` ใน `frontend/eslint.config.mjs` |
| TypeScript แบบ strict รวม `noUncheckedIndexedAccess` | `frontend/tsconfig.json` |
| endpoint ใหม่ปิดเป็นค่าตั้งต้น | `FallbackPolicy` ต้องเข้าสู่ระบบ ยกเว้นที่ใส่ `[AllowAnonymous]` |
| DI ที่ต่อผิดล้มตั้งแต่เปิด | `ValidateOnBuild` ใน Development และใน integration test |
| คีย์ JWT สั้นเกินหรือไม่มี API ไม่เปิด | `JwtOptions` ใช้ `ValidateOnStart` |

เมื่อ NuGet audit เตือนแพ็กเกจที่มาทางอ้อม ให้ตรึงรุ่นที่แก้แล้วในหมวด "Transitive security pins" ของ `Directory.Packages.props` แทนการปิดคำเตือน ตัวอย่างที่มีอยู่คือ `System.Security.Cryptography.Xml`

## ปัญหาที่เจอบ่อย

| อาการ | สาเหตุ | ทางแก้ |
|---|---|---|
| `docker compose up` ฐานข้อมูลไม่ขึ้น พอร์ตชน | เครื่องมี PostgreSQL ใช้ 5432 อยู่ | ดู [เมื่อพอร์ต 5432 ถูกใช้](#เมื่อพอร์ต-5432-ถูกใช้) |
| `password authentication failed for user "happypaws"` | `.env` ตั้งรหัสอื่น เช่น `change-me` แต่ API ใช้ `happypaws` หรือแก้ `.env` หลังสร้าง volume แล้ว (PostgreSQL อ่านรหัสตอนสร้าง volume ครั้งแรกเท่านั้น) | ตั้ง `ConnectionStrings__Postgres` ให้ตรง หรือ `docker compose down -v` เพื่อลบข้อมูลแล้วสร้างใหม่ |
| หน้าเว็บกดแล้วรอนาน โดยเฉพาะครั้งแรกที่เปิดแต่ละหน้า | `npm run dev` compile หน้านั้นตอนถูกเปิดครั้งแรก | ปกติของโหมด dev ซึ่งใช้ Turbopack อยู่แล้ว (`next dev --turbopack`) ครั้งต่อไปเร็วขึ้นเอง ระหว่างรอ เมนูมีวงหมุน และเนื้อหาเป็นโครงสีเทา (`loading.tsx`) ระบบที่ build แล้ว (`npm run build` แล้ว `npm start`) ไม่ต้อง compile ตอนเปิด |
| `migrate` พิมพ์ `fail: Microsoft.EntityFrameworkCore.Database.Command` สองบรรทัด | EF Core ลองอ่านตาราง `__migrations` ที่ยังไม่มีบนฐานข้อมูลว่าง | ไม่ใช่ปัญหา ถ้าบรรทัดสุดท้ายเป็น `Applied N migration(s)` |
| API ไม่เปิด แจ้ง `JwtOptions` ว่า `Issuer` หรือ `SigningKey` is required | ไม่ได้อยู่ใน Development เช่นรันด้วย `--no-launch-profile` หรือรัน dll ตรง จึงไม่ได้อ่าน `appsettings.Development.json` | ตั้ง `ASPNETCORE_ENVIRONMENT=Development` หรือใส่ `Auth__Jwt__Issuer` และ `Auth__Jwt__SigningKey` |
| หน้าเว็บขึ้น `Missing environment variable: API_BASE_URL` | ไม่มี `frontend/.env.local` | `cp .env.example .env.local` ในโฟลเดอร์ `frontend` |
| รหัส 2FA ไม่ผ่าน ขึ้นว่ารหัสไม่ถูกต้องหรือหมดอายุ | challenge มีอายุ 5 นาที หรือเวลาในเครื่องคลาดจากโทรศัพท์ | เริ่มเข้าสู่ระบบใหม่ ตรวจเวลาเครื่อง ถ้าความลับ TOTP หายใช้ `reset-credentials` |
| เข้าสู่ระบบได้ด้วยรหัสผ่านอย่างเดียว แต่ได้ 403 แทบทุกหน้า | บัญชียังไม่ได้ลงทะเบียน 2FA แต่ role มี scope ทางการแพทย์ ประตู 2FA ของ `[RequireScope]` จึงปฏิเสธ | ให้บัญชีนั้นลงทะเบียนเองด้วย `POST /me/2fa/enroll` แล้วเข้าสู่ระบบใหม่ หรือใช้ `reset-credentials --email <อีเมล>` ซึ่งออกความลับ TOTP ให้ |
| จองนัดไม่ได้เลย ได้ `slot_unavailable` ทุกช่วงเวลา | ยังไม่มีกะทำงาน ช่วงที่จองได้มาจากกะเท่านั้น | สร้างกะด้วย `POST /staff/schedules/shifts` ดู [ตารางเวร](features/schedules.md) |
| จ่ายห้องตรวจให้บัตรคิวไม่ได้ ไม่มีห้องให้เลือก | `bootstrap` ไม่ได้ส่ง `--branch-rooms` และยังไม่มี endpoint แก้สาขา | สร้างฐานข้อมูลใหม่โดยส่ง `--branch-rooms` |
| integration test ล้มตั้งแต่เริ่ม | Docker ไม่ได้เปิด Testcontainers สร้าง container ไม่ได้ | เปิด Docker Desktop แล้วรันใหม่ |
| ลูกค้าขอรหัส OTP แล้วไม่ได้รับ | ยังไม่มีตัวส่ง SMS | ดูรหัสใน log ของ API บรรทัด `Development only. OTP for ...` ต้องเปิด `Auth:SignIn:LogUndeliveredSecrets` (เปิดไว้แล้วใน Development) |
| ขอรหัส OTP ได้ `429` | ขอเกิน 3 ครั้งต่อเบอร์ใน 10 นาที | รอ 10 นาที หรือใช้เบอร์อื่นตอนทดลอง |
| ไฟล์ที่อัปโหลดไว้หายหลังรีสตาร์ทเครื่อง | ไม่ได้ตั้ง `Storage:LocalRoot` ไฟล์จึงอยู่ในโฟลเดอร์ชั่วคราวของระบบ | ตั้ง `Storage__LocalRoot` เป็นโฟลเดอร์ถาวร |
| `http://localhost:5080/` หรือ path ที่พิมพ์ผิดได้ 401 | ไม่มี token และ `FallbackPolicy` ตอบก่อนหา route | ใช้ path ที่ถูกต้อง เช่น `/v1/clinic` ซึ่งเปิดได้โดยไม่ต้องเข้าสู่ระบบ |
| `docker compose up --build` ล้มที่ `dotnet restore` ด้วย `NU1015` | Dockerfile ไม่ได้คัดลอก `Directory.Packages.props` (แก้แล้วใน `backend/Dockerfile` ถ้าเจออีกแปลว่าไฟล์ถูกแก้กลับ) | ให้ `COPY` ไฟล์นี้ก่อนบรรทัด restore |

## ทำงานกับสัญญา

`API Reference.dc.html` คือสัญญา ถ้าโค้ดกับสัญญาไม่ตรงกัน สัญญาชนะ

- **จุดกำกวม** บันทึกใน [จุดกำกวมของสัญญา](assumptions.md) แล้วทำตามการอ่านที่รักษาพฤติกรรมที่เขียนไว้
- **สิ่งที่ขาดไปเลย** รายงานและขออนุมัติก่อน แล้วบันทึกใน [endpoint ส่วนขยาย](contract-extensions.md) พร้อมเหตุผลและทางเลือกที่ไม่เลือก ใน controller ให้ระบุว่าเป็นส่วนขยายด้วย
- **ห้ามสร้าง endpoint เติมช่องว่างเอง** ถ้า endpoint ไม่อยู่ใน [แผนที่ endpoint](api-mapping.md) ถือว่าอยู่นอกขอบเขต

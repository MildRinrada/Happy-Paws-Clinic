# โครงสร้างโค้ด

เอกสารนี้บอกว่าโค้ดแต่ละส่วนอยู่ที่ไหน แบ่งหน้าที่อย่างไร และเพิ่ม use case หรือหน้าจอใหม่ต้องแตะไฟล์ไหนบ้าง ภาพใหญ่ของระบบอยู่ใน [สถาปัตยกรรม](architecture.md)

## โฟลเดอร์หลัก

```text
API Reference.dc.html   สัญญา API (ที่มาของทุก endpoint)
docker-compose.yml      postgres, api, web สำหรับรันบนเครื่อง
.env.example            ตัวแปรของ compose
backend/                API ASP.NET Core 10
frontend/               หน้าเว็บ Next.js 15
docs/                   เอกสาร
```

ไม่มีโฟลเดอร์ `database/` โครงสร้างฐานข้อมูลคือ migration ที่อยู่กับโค้ดที่สร้างมัน ดู [ฐานข้อมูล](database.md)

## ฝั่ง API (backend/)

```text
backend/
  HappyPaws.slnx               solution รูปแบบ XML ของ .NET 10 (4 โปรเจกต์หลัก 2 โปรเจกต์เทสต์)
  Directory.Build.props        net10.0, nullable, warning เป็น error, เปิดตัวตรวจ code style
  Directory.Packages.props     รุ่นของทุกแพ็กเกจ (central package management)
  .config/dotnet-tools.json    dotnet-ef 10.0.11 แบบ local tool
  Dockerfile                   build แล้วรัน HappyPaws.Api.dll (ตอนนี้ restore ไม่ผ่าน ดู development.md)
  src/
    HappyPaws.Domain/          entity และกฎ ไม่มีแพ็กเกจภายนอก
    HappyPaws.Application/     use case, DTO, validator, interface
    HappyPaws.Infrastructure/  EF Core, repository, JWT, TOTP, ตัวส่งข้อความ, SSE broker
    HappyPaws.Api/             controller และทุกเรื่อง HTTP
  tests/
    HappyPaws.UnitTests/         domain และ application ไม่ต้องมีฐานข้อมูล
    HappyPaws.IntegrationTests/  เรียก API จริงกับ PostgreSQL ใน Testcontainers
```

### HappyPaws.Api

```text
Program.cs            composition root และลำดับ middleware
Controllers/
  Public/             อ่านได้โดยไม่เข้าสู่ระบบ: clinic, services, booking/availability
  Auth/               login, 2FA, refresh, logout
  Account/            /me, /me/sessions และ /appointments/{id} ที่พนักงานกับลูกค้าใช้ร่วมกัน
  Staff/              /staff/* และการตั้งค่าคลินิก
  Portal/             /portal/* และ booking/holds ของลูกค้า
  Realtime/           SSE /queue/stream
  Webhooks/           ว่าง
Contracts/            รูปของ request และ response ฝั่ง HTTP (snake_case ตามสัญญา) และ error envelope
Authorization/        [RequireScope], ตัวตรวจ scope และ 2FA
Idempotency/          [Idempotent] และ middleware
Errors/               แปลง exception และ model state เป็น error envelope
Http/                 ICurrentUser, If-Match, คุกกี้ refresh, path base /v1
Extensions/           ตั้งค่า authentication, authorization, rate limit, OpenAPI
Middleware/           RequestContextMiddleware (request id)
Operator/             คำสั่ง migrate, bootstrap, reset-credentials, seed-articles, seed-staff, seed-treatment-templates, seed-contacts
```

controller แบ่งตาม **ผู้ใช้** ไม่ใช่ตามโมดูล เพราะสัญญาแยก `/staff/*`, `/portal/*` และเส้นสาธารณะออกจากกัน ลูกค้าที่อ่านใบแจ้งหนี้ของตัวเองกับแคชเชียร์ที่อ่านใบแจ้งหนี้ใช้ use case คนละตัวในโมดูล `Billing` เดียวกัน

### HappyPaws.Application

หนึ่งโฟลเดอร์ต่อหนึ่งโมดูล ในโมดูลมีหนึ่งโฟลเดอร์ต่อหนึ่ง use case

```text
Queueing/
  QueueingModule.cs           ลงทะเบียน handler ของโมดูล
  IQueueTicketRepository.cs   interface ที่ Infrastructure ทำให้
  IQueueEventStream.cs
  QueueAccess.cs              ตรวจสิทธิ์ระดับข้อมูลของบัตรคิว
  Dtos/QueueTicketDto.cs
  CallTicket/CallTicketHandler.cs
  SkipTicket/SkipTicketHandler.cs
  IssueWalkInTicket/IssueWalkInTicketHandler.cs
  ...
```

| ไฟล์ | หน้าที่ | ห้ามทำ |
|---|---|---|
| `<UseCase>/<UseCase>Handler.cs` | command record, validator (ถ้ามี) และ handler ของ use case เดียว | รู้จัก HTTP หรือ `DbContext` |
| `I<Something>Repository.cs` | สิ่งที่ use case ต้องการจากฐานข้อมูล | มี logic |
| `<Module>Access.cs` | โหลดข้อมูลพร้อมตรวจสิทธิ์ระดับข้อมูล | ข้ามการตรวจสาขาหรือความเป็นเจ้าของ |
| `Dtos/` | รูปข้อมูลที่ส่งกลับ ชื่อ property แปลงเป็น snake_case ตอน serialize | มีกฎธุรกิจ |
| `<Module>Module.cs` | `AddScoped` ของ handler ในโมดูล | ลงทะเบียน handler ของโมดูลอื่น |

`Common/` เก็บของที่ทุกโมดูลใช้: `Abstractions/` (`IUnitOfWork`, `IClock`, `ICurrentUser`), `Authorization/` (`BranchGuard`, `PetAccessGuard`), `Concurrency/`, `Errors/`, `Pagination/`, `Validation/`

โมดูลที่มีโค้ดแล้ว: `Access`, `Appointments`, `Auditing`, `Billing`, `Clinics`, `Consultations`, `Customers`, `Identity`, `MedicalRecords`, `Notifications`, `Patients`, `Queueing`, `ServiceCatalog`, `Staff` โมดูลที่ยังเป็นโฟลเดอร์ว่าง (มีแค่ `.gitkeep`): `Analytics`, `Automation`, `Crm`, `Integrations`, `Inventory`, `Laboratory`, `Messaging`, `Prescriptions`, `Reporting`, `Scheduling` การสั่งยาตอนนี้อยู่ใน `Consultations/AddPrescription/`

### HappyPaws.Domain

ชื่อโมดูลเดียวกับ Application entity เปลี่ยนสถานะผ่าน method ของตัวเองเท่านั้น เช่น `QueueTicket.Call(...)`, `Invoice.Pay(...)` state machine แยกไฟล์ เช่น `QueueTicketTransitions.cs` `Common/` มี `Entity` (id, version, เวลา), `EntityId` (สร้าง id แบบมี prefix), `Money` (satang) และ exception ของ domain

### HappyPaws.Infrastructure

```text
DependencyInjection.cs          AddInfrastructure(): ลงทะเบียนทุกอย่างของชั้นนี้
Persistence/
  HappyPawsDbContext.cs         สแกน configuration ทั้ง assembly ไม่มีรายการ DbSet ยาว
  Configurations/<โมดูล>/       IEntityTypeConfiguration ต่อ aggregate
  Repositories/<โมดูล>/         ที่เดียวที่ query
  Migrations/                   สร้างด้วย dotnet-ef แล้วตรวจก่อน commit
  UnitOfWork.cs, PostgresErrorTranslator.cs, DocumentSequence.cs
  Idempotency/, Interceptors/, Converters/
Identity/                       JWT, TOTP, password hasher
Notifications/                  ประตูความยินยอม และช่องทางที่ยังเป็น stub
Realtime/QueueEventBroker.cs    SSE ใน process
Auditing/AuditTrail.cs
Staff/LoggingStaffInvitationSender.cs
```

## ฝั่งหน้าเว็บ (frontend/)

```text
frontend/
  next.config.ts        output standalone สำหรับ Dockerfile
  eslint.config.mjs     ห้าม fetch นอก src/lib/api
  src/
    middleware.ts       ต่ออายุเซสชันก่อนหน้าพนักงาน หน้า portal /login และ /api/queue
    app/                URL ของหน้าจอ
      (public)/         หน้าแรก เกี่ยวกับคลินิก ติดต่อเรา ทีม บทความ
      (auth)/           เข้าสู่ระบบ และรับคำเชิญพนักงาน
      (staff)/          queue, appointments, customers, pets, consultations, billing, schedules
                        และ settings/ (clinic, hours, rules, services, contacts ใต้ layout แท็บเดียวกัน)
      (portal)/         portal ลูกค้า
      api/queue/stream/ route handler ส่งต่อ SSE
    features/<ชื่อ>/    หนึ่งโฟลเดอร์ต่อหนึ่งเรื่อง
    components/
      ui/               ปุ่ม การ์ด ช่องกรอก checkbox alert และสถานะการบันทึกของฟอร์ม
      layout/           staff-shell, site-header/, site-footer/, site-container
      shared/           image-slot.tsx
    hooks/              hook ที่หลาย feature ใช้ร่วม เช่น use-form-submit.ts ของฟอร์มตั้งค่า
    lib/
      api/              http-client, server-client, auth-transport, event-stream, api-error
      auth/             คุกกี้เซสชัน, refresh, scope
      actions/          ActionResult ของ server action
      format/           วันที่และเงิน
    config/             env, site-navigation, site-footer
    types/              pagination.ts
```

### ชั้นของโค้ดในแต่ละ feature

| ชั้น | ที่อยู่ | หน้าที่ | คู่กับฝั่ง API |
|---|---|---|---|
| Route | `src/app/**/page.tsx` | server component อ่านข้อมูลและตรวจ scope เพื่อ UX | controller |
| API | `features/<ชื่อ>/api/<คำสั่ง>.ts` | หนึ่งไฟล์ต่อหนึ่ง endpoint ใช้ `staffApi()` หรือ `publicApi()` | endpoint |
| Action | `features/<ชื่อ>/actions/` | server action: ตรวจ input ด้วย zod เรียก api แล้ว `revalidatePath` | ไม่มี |
| Lib | `features/<ชื่อ>/lib/` | รวมข้อมูลหลายเส้นให้หน้าจอ เช่น `load-queue-view.ts` และคำแปลสถานะ | ไม่มี |
| Component | `features/<ชื่อ>/components/` | ชิ้นส่วนแสดงผล รับข้อมูลทาง props | ไม่มี |
| Hook | `features/<ชื่อ>/hooks/` | logic ฝั่งเบราว์เซอร์ เช่น `use-queue-stream.ts` | ไม่มี |
| Types | `features/<ชื่อ>/types/<ชื่อ>.types.ts` | รูปข้อมูลจาก API ชื่อฟิลด์ snake_case | DTO |

feature ที่มีโค้ด: `about`, `account`, `appointments`, `articles`, `assistant`, `authentication`, `billing`, `booking`, `clinic`, `consultations`, `contact`, `customers`, `home`, `laboratory`, `pets`, `portal`, `prescriptions`, `privacy`, `queue`, `schedules`, `service-catalog`, `settings`, `staff`, `team` ส่วนโฟลเดอร์อื่นใน `features/` ยังมีแค่ `.gitkeep` `home` เป็น feature เดียวที่ไม่เรียก API เนื้อหาอยู่ใน `content/` ดู [หน้าแรกสาธารณะ](features/home-page.md)

## กติกาการเขียนโค้ด

### ทั้งสองฝั่ง

1. **ไม่มีไฟล์ยักษ์** ไม่มี `ClinicService.cs`, `api.ts`, `types.ts` หรือ `components.tsx` ที่รวมทุกอย่าง ไฟล์ควรอยู่ราว 200 ถึง 300 บรรทัด เกินได้ถ้ามีเหตุผล เช่น `CloseConsultationHandler.cs` (309 บรรทัด) ที่เป็น transaction เดียว แต่ห้ามรวมสองโดเมนในไฟล์เดียว
2. **สัญญาคือ `API Reference.dc.html`** ห้ามเปลี่ยนชื่อ path, method หรือรูป body จุดกำกวมบันทึกใน [จุดกำกวมของสัญญา](assumptions.md) ส่วน endpoint ที่ขาดต้องขออนุมัติแล้วบันทึกใน [endpoint ส่วนขยาย](contract-extensions.md)
3. **คอมเมนต์อธิบายเหตุผล** โค้ดเดิมเขียนคอมเมนต์ภาษาอังกฤษว่าทำไม ส่วนข้อความที่ผู้ใช้เห็นเป็นภาษาไทย

### ฝั่ง API

1. **controller ไม่มีกฎธุรกิจและไม่แตะ `DbContext`** แค่ bind, เรียก handler, แปลงผล
2. **โหลดข้อมูลข้างใน `ExecuteInTransactionAsync`** เพราะการ retry ล้าง change tracker ข้อมูลที่โหลดก่อนจะหลุดและการแก้ไขหายเงียบ
3. **ทุก action ที่เปลี่ยนข้อมูลสำคัญต้องเรียก `IAuditTrail.Record`** รายชื่อ action อยู่ใน `Domain/Auditing/AuditActions.cs`
4. **POST ที่สร้างเงิน นัด หรือคิว ใส่ `[Idempotent]`**
5. **ไม่เรียกบริการภายนอกใน transaction** ส่งหลัง commit และห้ามให้ความล้มเหลวของการส่งทำให้คำขอล้ม
6. **ส่งข้อความถึงลูกค้าผ่าน `INotificationDispatcher` เท่านั้น** ห้ามเรียก `INotificationChannel` ตรง
7. **เพิ่มรุ่นแพ็กเกจใน `Directory.Packages.props`** ใน `.csproj` ใส่ `PackageReference` แบบไม่มี `Version`

### ฝั่งหน้าเว็บ

1. **เรียก API ผ่าน `features/<ชื่อ>/api/` เท่านั้น** `fetch` ใช้ได้เฉพาะใน `src/lib/api/` (ESLint บังคับ)
2. **เบราว์เซอร์ไม่ถือ token** โค้ดใน `lib/api/auth-transport.ts` และ `lib/auth/session.ts` ห้าม import จาก client component
3. **การแก้ข้อมูลเป็น server action ที่คืน `ActionResult`** คำปฏิเสธจาก API เป็นข้อมูลที่ฟอร์มแสดง ไม่ throw ยกเว้น 401 ที่พาไปหน้าเข้าสู่ระบบ
4. **ซ่อนปุ่มตาม scope ด้วย `context.can(...)`** เป็นแค่ UX API ยังตรวจเองทุกคำขอ
5. **หนึ่ง component ต่อไฟล์** เนื้อหาที่แก้บ่อยแยกไว้ใน `content/` หรือ `config/`

## เพิ่ม use case ใหม่

1. อ่าน endpoint ใน `API Reference.dc.html` และดูว่าอยู่แถวไหนใน [แผนที่ endpoint](api-mapping.md)
2. เพิ่ม method หรือ state ใน entity ของ `HappyPaws.Domain/<โมดูล>/` พร้อม unit test ใน `tests/HappyPaws.UnitTests/Domain/`
3. สร้าง `HappyPaws.Application/<โมดูล>/<UseCase>/<UseCase>Handler.cs` มี command, validator และ handler แล้วลงทะเบียนใน `<Module>Module.cs`
4. ถ้าต้องการ query ใหม่ เพิ่มใน interface ของ repository แล้วทำใน `Infrastructure/Persistence/Repositories/<โมดูล>/`
5. ถ้าเปลี่ยนตาราง แก้ configuration แล้วสร้าง migration ตาม [ฐานข้อมูล](database.md#migration)
6. เพิ่ม action ใน controller ของผู้ใช้กลุ่มนั้น ใส่ `[RequireScope]` และ `[Idempotent]` ตามสัญญา request record อยู่ใน `Contracts/<โมดูล>/`
7. เขียน integration test ใน `tests/HappyPaws.IntegrationTests/Api/<โมดูล>/` อย่างน้อยกรณีสำเร็จ และกรณีที่ไม่มี scope ต้องได้ 403

## เพิ่มหน้าจอใหม่

1. เพิ่มไฟล์เรียก endpoint ใน `features/<ชื่อ>/api/` หนึ่งไฟล์ต่อหนึ่ง endpoint และ type ใน `types/`
2. ถ้ามีการแก้ข้อมูล เพิ่ม server action ใน `features/<ชื่อ>/actions/` ใช้ `runAction` และ `revalidatePath`
3. สร้าง `src/app/(staff)/<path>/page.tsx` เป็น server component ใช้ `loadStaffContext()` และ `authed(...)`
4. เพิ่ม path ใน `matcher` ของ `src/middleware.ts` ไม่อย่างนั้นหน้านั้นจะไม่ได้ต่ออายุเซสชัน
5. เพิ่มเมนูใน `src/components/layout/staff-shell.tsx`
6. ตรวจด้วย `npm run typecheck` และ `npm run lint`

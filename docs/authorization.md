# สิทธิ์และการให้สิทธิ์

เอกสารนี้บอกว่าเมื่อรู้แล้วว่าผู้เรียกเป็นใคร API ตัดสินอย่างไรว่าทำอะไรได้ การพิสูจน์ตัวตนอยู่ใน [การเข้าสู่ระบบและเซสชัน](authentication.md) วิธีจัดการพนักงานและ role จากมุมของผู้ใช้อยู่ใน [พนักงานและ role](features/staff-and-roles.md)

**ด่านจริงคือ API** หน้าเว็บซ่อนปุ่มตามสิทธิ์เพื่อความสะดวกเท่านั้น ทุกกฎในเอกสารนี้ตรวจที่ backend ทุกคำขอ

## สามด่านที่ทุกคำขอต้องผ่าน

```text
1. ยืนยันตัวตนแล้วหรือยัง        ไม่ผ่าน  401 unauthenticated
2. token ถือ scope ที่ต้องใช้ไหม   ไม่ผ่าน  403 insufficient_scope (บอก scope ที่ขาด)
   และผ่าน 2FA ถ้าถือ scope อ่อนไหว ไม่ผ่าน  403 forbidden (details.required = two_factor)
3. ทำกับข้อมูลแถวนี้ได้ไหม         ไม่ผ่าน  403 forbidden หรือ 404 not_found
```

ด่าน 1 และ 2 ตัดสินจาก token อย่างเดียว อยู่ใน `HappyPaws.Api/Authorization/` ด่าน 3 ต้องเห็นข้อมูลก่อน จึงอยู่ใน use case ของชั้น Application ทั้งสามด่านต้องผ่านครบ ด่านหนึ่งแทนอีกด่านไม่ได้ เช่น สัตวแพทย์ที่ถือ `record:read` อ่านเวชระเบียนได้เฉพาะสาขาที่ตัวเองประจำ

## Scope

สิทธิ์เขียนเป็น `resource:action` เช่น `queue:call` รายการทั้งหมดอยู่ที่ `Domain/Access/Scopes.cs` มี 31 กลุ่ม 29 กลุ่มคัดลอกจาก contract ตรงตัว ตั้งแต่ `user`, `settings`, `staff`, `role`, `customer`, `patient`, `record` ไปจนถึง `invoice`, `payment`, `inventory`, `audit` อีกสองกลุ่มคือ `content` และ `ai` ดูหัวข้อถัดไป

- `ScopeCatalogue` อ่านรายการด้วย reflection จากค่าคงที่ใน `Scopes` scope ที่ endpoint ต้องใช้จึงเป็น scope ที่ role ได้รับได้เสมอ ไม่มีรายการที่สองให้ลืมแก้
- `GET /staff/permissions` คืนรายการนี้ แยก resource กับ action
- `portal` ไม่อยู่ในแคตตาล็อก เป็นเครื่องหมายของฝั่งลูกค้า ได้มาจากการมี membership ลูกค้า ไม่ใช่จาก role ที่คลินิกแก้ได้
- scope หนึ่งตัวมีชื่อไม่ตรงกลุ่ม คือ `Scopes.Payment.Payout` มีค่าเป็น `billing:payout` ตาม contract

### scope ที่ไม่อยู่ใน contract

รายการ scope ของ contract ปิดตาย ส่วนขยายทุกข้อจึงยืม scope ที่มีอยู่ ยกเว้น scope ในตารางนี้ที่เจ้าของ contract อนุมัติเพิ่มทีละตัว

| Scope | ใช้กับ | ใครได้ตั้งแต่แรก | เหตุผล |
|---|---|---|---|
| `content:manage` | ทุกเส้นใต้ `/staff/articles` ทั้งอ่านร่างและเขียน ([contract-extensions.md § 26](./contract-extensions.md)) | `clinic_owner` ผ่าน `*` และ `clinic_admin` | การพูดในนามคลินิกบนหน้าเว็บไม่ตรงกับ scope ใดเดิม ถ้ายืม `settings:write` คนเขียนบทความจะแก้กติกาของคลินิกได้ด้วย |
| `ai:use` | ทุกเส้นใต้ `/staff/ai` ที่เรียกโมเดล และ `GET /staff/ai/usage` คู่กับ scope ของข้อมูลที่อ่าน ([contract-extensions.md § 34](./contract-extensions.md)) | `clinic_owner` ผ่าน `*`, `veterinarian` และ `receptionist` | ทุกการใช้กินโควตารายเดือนที่ผู้ให้บริการระบบจ่าย จึงต้องถอดออกจาก role ได้แยกจากสิทธิ์อ่านข้อมูล และไม่เคยให้อ่านอะไรเกิน scope ของข้อมูลที่ถืออยู่ API key ถือไม่ได้ |

- ไม่ใช่ scope อ่อนไหว ไม่ต้องผ่าน 2FA
- `clinic_admin` ของคลินิกที่ตั้งระบบก่อนรุ่นนี้ได้ `content:manage` จาก migration `StaffArticles` เพราะ role นี้ขยายจากแคตตาล็อกครั้งเดียวตอน `bootstrap` (ดูหัวข้อ Role) role อื่นไม่ได้อะไรเพิ่ม เจ้าของคลินิกให้เองได้ที่หน้าพนักงานและสิทธิ์ ตามกติกาเมื่อให้สิทธิ์ปกติ
- หน้าเว็บมีค่าคงที่ `Scope.ContentManage` และป้าย "บทความบนเว็บไซต์" ในหน้าแก้ role
- `ai:use` ไม่ใช่ scope อ่อนไหวเช่นกัน migration `StaffAiAssistant` ให้ `veterinarian` และ `receptionist` ที่เป็น role ตั้งต้นของคลินิกเดิม role ตั้งต้นของคลินิกใหม่มีให้เอง `clinic_admin` ไม่ได้ เพราะเจ้าของงานอนุมัติให้หมอ เคาน์เตอร์ และเจ้าของคลินิกเท่านั้น ผู้ดูแลเพิ่มให้ role ไหนก็ได้เอง หน้าเว็บมี `Scope.AiUse` และป้าย "ผู้ช่วย AI"

### Wildcard

| รูปแบบ | ความหมาย | ตัวอย่าง |
|---|---|---|
| `*` | ทุก scope | `clinic_owner` |
| `resource:*` | ทุก action ของ resource นั้น | `consult:*`, `queue:*`, `invoice:*` |

`ScopeSet` เป็นที่เดียวในระบบที่ตอบว่าถือ scope ไหม ใช้ทั้งที่ด่าน API ที่ `/me/capabilities` และตอนจัดการ role wildcard ขยายตอนตรวจ ไม่ใช่ตอนบันทึก role ดังนั้น scope ที่เพิ่มใน `invoice` ภายหลังจะครอบโดย `invoice:*` เดิมเอง `payment:*` ไม่ครอบ `billing:payout` เพราะคนละ resource

## Endpoint ประกาศสิทธิ์อย่างไร

```csharp
[HttpPost("tickets")]
[Idempotent]
[RequireScope(Scopes.Queue.Write)]
public async Task<IActionResult> IssueTicket(...)
```

- `RequireScopeAttribute` ตั้งชื่อ policy เป็น `scope:<ชื่อ scope>`
- `ScopePolicyProvider` สร้าง policy ตามชื่อนั้นตอนต้องใช้ ทุก policy มีทั้งข้อกำหนด scope และข้อกำหนด 2FA ไม่มีทะเบียน policy กลางให้แก้เมื่อเพิ่ม endpoint
- `ScopeAuthorizationHandler` ตรวจ scope ผ่าน `ScopeSet` ตั้งใจไม่เรียก `Fail()` เพื่อให้ `ScopeAuthorizationResultHandler` ตอบ 403 พร้อม `details.missing_scopes` ได้
- **ห้ามตรวจชื่อ role ในโค้ด** ใช้ `[RequireScope(...)]` เสมอ ไม่ใช้ `[Authorize(Roles = ...)]` เพราะคลินิกสร้าง role ใหม่ได้ เมื่อมี role ชื่อใหม่ การตรวจชื่อ role จะผิดทันที ส่วนการตรวจ scope ยังถูก

### ปิดไว้ก่อนเป็นค่าเริ่มต้น

`AuthorizationExtensions` ตั้ง fallback policy ให้ต้องยืนยันตัวตน endpoint ที่ลืมใส่ attribute จึงถูกปิด ไม่ใช่ถูกเปิด endpoint ที่เปิดให้คนที่ยังไม่เข้าสู่ระบบต้องใส่ `[AllowAnonymous]` เอง ตอนนี้มีเท่านี้

| Endpoint | ทำไมเปิด | ตรวจอะไรแทน |
|---|---|---|
| `GET /clinic`, `/clinic/branches`, `/clinic/hours` | ข้อมูลสาธารณะตาม contract | ไม่มี |
| `GET /services`, `/services/categories` | ข้อมูลสาธารณะตาม contract | ไม่มี |
| `GET /booking/availability` | ดูช่วงว่างก่อนเข้าสู่ระบบ | ไม่มี |
| `POST /auth/staff/login`, `/auth/2fa/verify`, `/auth/refresh`, `/auth/logout` | ยังไม่มี token | รหัสผ่าน challenge หรือ refresh token |
| `POST /staff/members/invitations/{id}/accept` | ผู้ถูกเชิญยังไม่มีบัญชี | token ในลิงก์ |
| `POST /queue/displays/claim` | ทีวียังไม่มี credential (ส่วนขยาย § 30) | รหัสตั้งจอที่ใช้ได้ครั้งเดียวภายใน 30 นาที และเพดาน 30 ครั้งต่อชั่วโมงต่อ IP |
| `GET /staff/integrations/calendar/ics` | แอปปฏิทินส่ง bearer token ไม่ได้ | token ในลิงก์ หรือพนักงานที่เข้าสู่ระบบอยู่ ตรวจใน `CalendarFeedHandler` ดู [การเชื่อมต่อระบบ](features/integrations.md) |

### Endpoint ที่ยืนยันตัวตนแล้วแต่ไม่มี `RequireScope`

บาง endpoint ใช้ร่วมกันระหว่างพนักงานกับลูกค้า หรือเป็นเรื่องของผู้เรียกเอง จึงไม่ใส่ scope และตัดสินใน use case แทน

| Endpoint | ตัดสินอย่างไร |
|---|---|
| `/me`, `/me/capabilities`, `/me/password`, `/me/sessions` | เป็นข้อมูลของบัญชีผู้เรียกเอง |
| `GET /appointments/{id}` และ `reschedule`, `cancel`, `confirm`, `check-in` | `AppointmentAccess` รับพนักงานด้วย scope และสาขา รับลูกค้าด้วยการดูแลสัตว์ตัวนั้น คนอื่นได้ 404 |
| `GET /queue/stream` | พนักงานต้องมี `queue:read` และอยู่สาขานั้น ลูกค้าเห็นเฉพาะเหตุการณ์ของคิวตัวเอง token ของจอเห็นเฉพาะสาขาของจอ |
| `GET /queue/display-board` | token ของจอเท่านั้น คนอื่นได้ 403 (ส่วนขยาย § 30) |
| `GET`/`PUT /portal/consents`, `POST /portal/data-requests` | ใช้รหัสลูกค้าจาก token (`RequireCustomerId`) ไม่รับจาก request |

ข้อสังเกต endpoint กลุ่มนี้ไม่ผ่านด่าน 2FA เพราะด่าน 2FA ติดมากับ policy ของ `RequireScope` เท่านั้น

### API key

เครื่องที่เรียกด้วย API key (`Authorization: Bearer hp_...`) ได้ principal รูปเดียวกับ token ของพนักงาน คือบัญชีและสมาชิกพนักงานของผู้สร้าง scope และสาขาของ key ที่ผู้สร้างยังถืออยู่ และ claim `key` ที่บอกว่าเป็น key ไหน ด่านทั้งสามทำงานเหมือนเดิม ต่างกันสองข้อ

- **key ใช้ได้เฉพาะ endpoint ที่มี `RequireScope`** default policy และ fallback policy มี `InteractiveCallerRequirement` ที่ปฏิเสธ key ด้วย `403 forbidden` (`details.required = interactive_session`) endpoint ในตารางข้างบนจึงใช้ key ไม่ได้ ไม่อย่างนั้น key ที่มี scope เดียวก็แก้โปรไฟล์หรือเพิกถอนเซสชันของผู้สร้างได้
- **key ไม่มี 2FA** จึงถือ scope ระดับ 2FA ไม่ได้ตั้งแต่ตอนสร้าง (`ApiKeyScopeRules`) แทนที่จะได้รับการยกเว้น

รายละเอียดอยู่ใน [การเชื่อมต่อระบบภายนอก](security/integrations.md)

### token ของจอห้องรอ

จอห้องรอที่ตั้งด้วยรหัสตั้งจอเรียกด้วย `Authorization: Bearer hpdsp_...` ([ส่วนขยาย § 30](./contract-extensions.md)) ต่างจาก API key ตรงที่ไม่ได้ทำแทนใคร principal มีแค่รหัสจอกับสาขาเดียว ไม่มีบัญชี สมาชิก หรือ scope

- **เปิดทีละเส้น** `DisplayTokenAuthenticationHandler` ยืนยันตัวให้เฉพาะ route ที่ติด `[AcceptsDisplayToken]` คือ `GET /queue/display-board` และ `GET /queue/stream` route อื่นทุกเส้นได้ `401` ตั้งแต่ขั้นยืนยันตัว ก่อนถึง fallback policy หรือ `RequireScope`
- **กุญแจดอกที่สอง** `InteractiveCallerRequirement` ปฏิเสธ principal ของจอบน route ที่ไม่ติดเครื่องหมายด้วย ถ้าวันหนึ่งขั้นยืนยันตัวพลาด จอก็ยังไม่ผ่าน default และ fallback policy
- **เส้นสาธารณะ** token ไม่ถูกนับ คำตอบเหมือนคนที่ไม่ได้เข้าสู่ระบบ

## 2FA เป็นเงื่อนไขของสิทธิ์

contract กำหนดว่าพนักงานที่มีสิทธิ์ระดับเวชระเบียนขึ้นไปต้องผ่าน 2FA ระบบตัดสินจาก **scope บน token** ไม่ใช่ชื่อ role หรือ `staff_type` role ที่คลินิกสร้างเองแล้วให้ `record:write` จึงต้องผ่าน 2FA ตามไปด้วย

scope ที่ถือว่าอ่อนไหว (`TwoFactorAuthorizationHandler.SensitiveScopes`)

| กลุ่ม | scope |
|---|---|
| เวชระเบียน | `record:read`, `record:write`, `record:sign`, `record:amend`, `record:audit` |
| การรักษา | `consult:write`, `rx:write`, `rx:dispense` |
| เงิน | `payment:refund`, `billing:payout` |
| การจัดการระบบ | `settings:danger`, `user:manage`, `role:manage` |

กติกาคือ ถ้า token ถือ scope อ่อนไหวตัวใดก็ตาม (รวมผ่าน wildcard) และ token ไม่ได้ผ่าน 2FA ทุก endpoint ที่มี `RequireScope` ปฏิเสธ แม้ endpoint นั้นต้องการเพียง `queue:read` เพราะเงื่อนไขดูทั้ง token ไม่ใช่เฉพาะ scope ของ endpoint คำตอบคือ

```json
{
  "error": {
    "code": "forbidden",
    "message": "ต้องยืนยันตัวตนสองขั้นตอนก่อนใช้งานส่วนนี้",
    "message_en": "Two-factor authentication is required for this scope level.",
    "request_id": "req_3f9a1c0b2d4e",
    "details": { "required": "two_factor" }
  }
}
```

ตาม role ตั้งต้น `clinic_owner`, `clinic_admin`, `veterinarian` และ `cashier` (ผ่าน `payment:*`) ต้องผ่าน 2FA ส่วน `receptionist` และ `vet_assistant` ไม่ถือ scope อ่อนไหว ค่า `Auth:TwoFactorRequiredForMedicalScopes` ใน `appsettings.json` ไม่มีโค้ดใดอ่าน กฎนี้เปิดตลอด

## Role

role คือชุด scope ที่เก็บเป็นข้อมูล (`Role`) contract ให้ role ตั้งต้น 7 ตัว และให้เจ้าของคลินิกสร้างเพิ่มเองได้ คำสั่ง `bootstrap` สร้าง role ตั้งต้นจาก `DefaultRoles.StarterScopes` ครั้งเดียว หลังจากนั้นแถวเป็นของคลินิก ตารางนี้ไม่ถูกใช้ตัดสินสิทธิ์อีก

| Role | Scope ตั้งต้น |
|---|---|
| `clinic_owner` | `*` |
| `clinic_admin` | ทุก scope ในแคตตาล็อก ยกเว้น `billing:payout`, `settings:danger` และ `ai:use` รวม `content:manage` |
| `veterinarian` | `consult:*`, `record:write`, `lab:*`, `rx:write`, `queue:call` และ `ai:use` (§ 34) |
| `receptionist` | `appointment:*`, `queue:*`, `customer:*`, `invoice:collect` และ `ai:use` (§ 34) |
| `vet_assistant` | `queue:read`, `patient:read`, `vitals:write` |
| `cashier` | `invoice:*`, `payment:*`, `receipt:issue` |
| `customer` | `portal` |

- `clinic_admin` ขยายจากแคตตาล็อกตอน seed จึง **ไม่** ได้ scope ที่เพิ่มในรุ่นหลังเอง ต่างจาก role ที่ใช้ wildcard scope ที่เพิ่มภายหลังต้องให้ผ่าน migration อย่างที่ `StaffArticles` ทำกับ `content:manage`
- role ตั้งต้นทั้ง 7 มีธง `IsSystem` แก้ scope ได้ ตอนนี้ยังไม่มี endpoint ลบ role ใดเลย
- role `customer` อธิบายฝั่งลูกค้า ไม่ใช่ตำแหน่งงาน ให้พนักงานไม่ได้ และแก้ scope ไม่ได้
- ตาราง seed ตามที่ contract เขียน ทำให้ `veterinarian` ไม่มี `record:sign` จึงปิดเคสไม่ได้ และ `receptionist` มี `invoice:collect` แต่ endpoint รับเงินต้องการ `payment:collect` รายละเอียดอยู่ใน [ข้อตีความ](assumptions.md)
- `PUT /staff/roles/{id}/permissions` แทนที่ scope ทั้งชุด scope ที่ไม่อยู่ในแคตตาล็อกถูกปฏิเสธพร้อมชื่อ รองรับ `If-Match`

## กติกาเมื่อให้สิทธิ์

การจัดการสิทธิ์ก็คือสิทธิ์อย่างหนึ่ง กติกาเหล่านี้ใช้ทุกที่ที่ role หรือ membership เปลี่ยน คือสร้าง role แก้ scope ของ role เชิญพนักงาน เปลี่ยน role ของพนักงาน และปิดบัญชีพนักงาน

| กติกา | ที่อยู่ | ผลเมื่อผิด |
|---|---|---|
| ให้ได้เฉพาะ scope ที่ตัวเองถือ ทั้งตอนให้และตอนถอดออก wildcard ให้ได้เฉพาะคนที่ถือ wildcard นั้นหรือ `*` | `Application/Access/ScopeGrantPolicy.cs` | `403 insufficient_scope` บอก scope ที่ขาด |
| แก้หรือปิดบัญชีพนักงานที่ถือ scope มากกว่าตัวเองไม่ได้ | `Application/Staff/StaffMemberAccess.cs` | `403 insufficient_scope` |
| ผู้จัดการที่ผูกสาขา เห็นและแก้ได้เฉพาะพนักงานที่อยู่สาขาเดียวกันอย่างน้อยหนึ่งสาขา | `StaffMemberAccess` | `404 not_found` |
| ต้องมีพนักงาน active ที่ถือ `*` อย่างน้อยหนึ่งคนเสมอ | `Application/Access/OwnerLockoutGuard.cs` | `409 last_owner` |
| ปิดบัญชีตัวเองไม่ได้ | `DeactivateStaffMemberHandler` | `409 cannot_deactivate_self` |

ถ้าไม่มีกติกาข้อแรก `role:manage` ก็คือ `*` ที่ต้องกดเพิ่มอีกขั้นเดียว ส่วนข้อ `last_owner` สำคัญเพราะระบบไม่มีผู้ดูแลแพลตฟอร์มที่จะมาแก้ให้ ถ้าเจ้าของคนสุดท้ายหายไป ทางกลับมีเพียงคำสั่งบนเซิร์ฟเวอร์ `OwnerLockoutGuard` ถามฐานข้อมูลก่อนและหลังการเปลี่ยนใน transaction เดียว และปฏิเสธเฉพาะการเปลี่ยนที่ทำให้จากมีเจ้าของกลายเป็นไม่มี

## กฎระดับข้อมูล

กฎที่ scope แสดงไม่ได้ ต้องตรวจใน use case หลังโหลดข้อมูล

| กฎ | ใช้ที่ | ผลเมื่อผิด |
|---|---|---|
| พนักงานทำงานได้เฉพาะสาขาที่ประจำ ผู้ถือ `*` หรือ token ที่ไม่ผูกสาขาเลยไม่ถูกจำกัด (`BranchGuard`) | คิว นัด เวชระเบียน ใบแจ้งหนี้ พนักงาน audit log | ข้อมูลรายแถวตอบ `403 forbidden` รายการถูกกรองที่ query |
| ลูกค้าถึงได้เฉพาะสัตว์ที่ตัวเองเป็นเจ้าของหรือผู้ดูแลร่วม ตรวจกับฐานข้อมูลทุกครั้ง (`PetAccessGuard`, `IPetAccessPolicy`) | ทุก `/portal/*` ที่รับรหัสสัตว์ และนัดที่ใช้ร่วมกัน | `404 not_found` |
| ลูกค้าดูประวัติการรักษาได้เฉพาะเมื่อลิงก์ผู้ดูแลเปิดสิทธิ์ดูประวัติ (`EnsureMayViewHistoryAsync`) | เตรียมไว้ให้ `GET /portal/pets/{id}/records` ซึ่งยังไม่ได้ทำ | `404 not_found` |
| ลูกค้าเลื่อนนัดได้ก่อนถึงเวลาตัดที่คลินิกตั้ง (`CancellationCutoffHours`) ส่วนการยกเลิกยังไม่ได้ตรวจเวลาตัด | `reschedule` | `409 cutoff_passed` พร้อมเวลาตัดใน `details` |
| เวชระเบียนที่ลงนามแล้วแก้ตรงไม่ได้ ต้องแก้ผ่าน amendment | การตรวจและเวชระเบียน | `423 record_locked` |
| รหัสลูกค้าของ portal มาจาก token เท่านั้น | `/portal/consents`, `/portal/data-requests` | ไม่มีช่องให้ส่งรหัสคนอื่น |

ความเป็นเจ้าของสัตว์เป็นแบบหลายต่อหลายและเพิกถอนได้ (`POST`/`DELETE /staff/pets/{id}/owners`) จึงไม่เก็บใน token ถ้าเก็บ ผู้ดูแลร่วมที่ถูกถอนจะยังเข้าถึงได้ต่อจน token หมดอายุ ส่วนสาขาอยู่ใน token (`brn`) เพราะเปลี่ยนไม่บ่อย จึงยอมให้มีผลภายใน 15 นาที

### เลือก 403 หรือ 404

| สถานการณ์ | ตอบ | เหตุผล |
|---|---|---|
| token หมด ถูกเพิกถอน หรือไม่มี | 401 | หน้าเว็บลอง refresh เมื่อเจอ 401 |
| token ใช้ได้แต่ขาด scope | 403 `insufficient_scope` | ผู้ใช้รู้อยู่แล้วว่า endpoint มีอยู่ บอกว่าขาดอะไร |
| พนักงานขอข้อมูลของสาขาอื่น | 403 `forbidden` | พนักงานมีสิทธิ์รู้ว่าข้อมูลมีอยู่ |
| ลูกค้าขอข้อมูลของคนอื่น หรือขอเซสชันของบัญชีอื่น | 404 `not_found` | ตอบ 403 เท่ากับยืนยันว่ารหัสนั้นมีจริง |

ห้ามใช้ 401 แทน "รู้ว่าเป็นใครแต่ไม่ให้" เพราะหน้าเว็บจะ refresh วนไม่จบ `ScopeAuthorizationResultHandler` แยกสองกรณีนี้ให้แล้ว เหตุผลภายในของ `ForbiddenException` และรหัสใน `NotFoundException` ไม่ถูกส่งกลับไปให้ผู้เรียก

## สิทธิ์บนหน้าเว็บ

หน้าเว็บอ่าน scope จาก `GET /me` แล้วตัดสินด้วย `allows()` ใน `frontend/src/lib/auth/scopes.ts` ซึ่งใช้กฎ wildcard แบบเดียวกับ `ScopeSet` `loadStaffContext()` ส่ง `can(scope)` ให้ทุกหน้าในคอนโซล

- กระดานคิวซ่อนปุ่มเรียกจากคนที่ไม่มี `queue:call`
- หน้าเก็บเงินซ่อนปุ่มรับเงินจากคนที่ไม่มี `payment:collect`
- กระดานคิวลดระดับแทนที่จะพัง ผู้ช่วยสัตวแพทย์มี `queue:read` และ `patient:read` แต่ไม่มี `staff:read` จึงเห็นชื่อสัตว์แต่เห็นสัตวแพทย์เป็นรหัส

หน้าเว็บเขียนโดยถือว่าผู้ใช้เรียก endpoint ตรงได้เสมอ 403 จาก API เป็นคำตอบที่รองรับ ไม่ใช่ error ที่ไม่คาดคิด

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| attribute, policy, handler | `backend/src/HappyPaws.Api/Authorization/` |
| ลงทะเบียนและ fallback policy | `backend/src/HappyPaws.Api/Extensions/AuthorizationExtensions.cs` |
| scope, wildcard, role ตั้งต้น | `backend/src/HappyPaws.Domain/Access/` (`Scopes.cs`, `ScopeSet.cs`, `ScopeCatalogue.cs`, `DefaultRoles.cs`, `Role.cs`) |
| scope ที่ใส่ใน token | `backend/src/HappyPaws.Application/Identity/Capabilities/EffectiveScopeResolver.cs` |
| กติกาเมื่อให้สิทธิ์ | `backend/src/HappyPaws.Application/Access/`, `backend/src/HappyPaws.Application/Staff/StaffMemberAccess.cs` |
| กฎระดับข้อมูล | `backend/src/HappyPaws.Application/Common/Authorization/`, `backend/src/HappyPaws.Application/Appointments/AppointmentAccess.cs` |
| หน้าเว็บ | `frontend/src/lib/auth/scopes.ts`, `frontend/src/features/account/lib/staff-context.ts` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/AuthChainTests.cs`, `Api/Staff/`, `backend/tests/HappyPaws.UnitTests/Domain/Access/` |

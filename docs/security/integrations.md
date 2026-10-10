# การเชื่อมต่อระบบภายนอก

webhook ขาออก API key ฟีดปฏิทิน token ของจอห้องรอ และค่าตั้งของผู้ให้บริการ เป็นทางที่ข้อมูลออกจากระบบ หรือที่เครื่องอื่นเข้ามาโดยไม่มีคนเข้าสู่ระบบ หน้านี้บอกว่าแต่ละทางกันอะไรไว้ การทำงานทั้งหมดอยู่ใน [การเชื่อมต่อระบบ](../features/integrations.md)

## SSRF ของ webhook

URL ของ webhook มาจากคนพิมพ์ แต่เซิร์ฟเวอร์เป็นผู้เรียกจากในเครือข่ายของคลินิก ถ้าไม่กัน ใครที่มี `integration:manage` ก็สั่งให้เซิร์ฟเวอร์ยิงเข้าฐานข้อมูล เราเตอร์ หรือบริการ metadata ของ cloud (`169.254.169.254`) ซึ่งคืน credential ของเครื่องได้

| ชั้น | ที่ไหน | ทำอะไร |
|---|---|---|
| ตอนสร้างและตอนแก้ URL | `Domain/Integrations/WebhookUrl.cs` | บังคับ `https` ไม่มี user info ไม่มี fragment ไม่รับ `localhost` ชื่อที่ลงท้าย `.local`, `.localhost`, `.internal` และ IP ที่ไม่ใช่สาธารณะ `PATCH /staff/webhooks/{id}` ใช้ฟังก์ชันเดียวกับ `POST` ไม่ใช่สำเนา |
| ก่อนส่ง | `Infrastructure/Webhooks/WebhookAddressGuard.cs` | resolve ชื่อ ถ้าที่อยู่ **ใดก็ได้** ไม่ใช่สาธารณะ ไม่ส่ง (`address_blocked`) |
| ตอนต่อ socket | `Infrastructure/Webhooks/WebhookHttpHandler.cs` | connect callback resolve และตรวจซ้ำ แล้วต่อไปยังที่อยู่ที่เพิ่งตรวจ กัน DNS rebinding |
| คำตอบ | `WebhookHttpHandler.cs`, `WebhookHttpSender.cs` | ไม่ตาม redirect ไม่ใช้ proxy ไม่ใช้คุกกี้ timeout ทั้งครั้ง อ่านคำตอบไม่เกิน 4 KB เก็บ 1,000 ตัวอักษร |

กติกาของที่อยู่อยู่ใน `Domain/Integrations/WebhookAddressRules.cs` ที่เดียว ปฏิเสธ loopback, `0/8`, `10/8`, `100.64/10`, `127/8`, `169.254/16`, `172.16/12`, `192.0.0/24`, `192.168/16`, `198.18/15`, multicast และช่วงสงวน ฝั่ง IPv6 ปฏิเสธ `::`, `::1`, `fc00::/7`, `fe80::/10`, `fec0::/10`, multicast และตรวจ IPv4 ที่ซ่อนอยู่ใน `::ffff:0:0/96`, `::/96` และ `64:ff9b::/96` ตามกติกา IPv4

Development ยอม `http://localhost` และ loopback (ปิดได้ด้วย `Webhooks:AllowLocalhostInDevelopment`) ช่วงส่วนตัวและ metadata ปฏิเสธทุก environment ไม่มีค่าตั้งที่เปิดได้

URL ของ API แล็บ (`PUT /staff/integrations/lab`) ใช้กติกาตอนสร้างชุดเดียวกัน เพราะตัวเชื่อมในอนาคตจะเรียกจากในเครือข่ายเช่นกัน

## ลายเซ็น webhook

- HMAC-SHA256 ด้วย secret ของ webhook บน `<timestamp>.<body>` ส่งใน `X-Signature: sha256=<hex>` พร้อม `X-Webhook-Timestamp` การลงนาม timestamp ด้วยทำให้ผู้รับปฏิเสธคำขอที่ถูกดักแล้วส่งซ้ำได้
- secret สุ่ม 256 bit สร้างฝั่งเซิร์ฟเวอร์ (`whsec_...`) แสดงครั้งเดียวตอนสร้าง หรือตอนหมุน (`POST /staff/webhooks/{id}/rotate-secret`) คำตอบมี `Cache-Control: no-store` รายการ webhook และ audit ไม่มี secret
- หมุนแล้ว secret เดิมหยุดลงนามทันที ไม่มีช่วงซ้อน ปลายทางที่ยังใช้ค่าเดิมจะปฏิเสธ และการส่งลองใหม่ตามรอบปกติ
- secret ต้องอ่านกลับได้เพื่อใช้ลงนาม จึงเก็บแบบเข้ารหัสด้วย Data Protection ไม่ใช่ hash ([ข้อมูลและความลับ](data-and-secrets.md#ความลับที่เข้ารหัสด้วย-data-protection))
- ไม่มีการส่งแบบไม่ลงนาม ถ้าถอดรหัส secret ไม่ได้ การส่งล้มเหลวด้วย `secret_unavailable`

## ปิดและลบ webhook

- ปิด (`active: false`) หยุดการแตก event และหยุด delivery ที่รอส่งทันทีใน transaction เดียว ตัวส่งตรวจซ้ำก่อนส่งทุกครั้ง แถวที่ถูกจองไปก่อนกดปิดจึงไม่ถูกส่ง
- ลบแล้วแถวยังอยู่เพื่อเก็บประวัติการส่ง แต่ secret ที่เข้ารหัสถูกล้างเป็นค่าว่าง และ query string ถูกตัดออกจาก URL เพราะอาจมี token ของผู้รับ ไม่มีความลับเหลือในแถวที่ลบแล้ว
- ทุกการแก้ ลบ หมุน secret และส่งซ้ำเขียน audit ปลายทางใน audit ไม่มี query string

## ข้อมูลที่ออกไปกับ webhook

event ทุกตัวเป็นแบบบาง มีแค่รหัส เวลา สถานะ และยอดเงิน ไม่มีข้อมูลสุขภาพและข้อมูลติดต่อ รายการเต็มต่อ event อยู่ใน [การเชื่อมต่อระบบ](../features/integrations.md#event-แต่ละตัวพาอะไรไป) payload ทุกตัวสร้างในไฟล์เดียว `Application/Integrations/Webhooks/WebhookPayloads.cs` เทสต์ยืนยันว่าผลวินิจฉัย อาการที่แจ้ง ชื่อลูกค้า ชื่อสัตว์ และรายการในบิลไม่อยู่ใน body

outbox เขียนใน transaction เดียวกับงาน การเปลี่ยนแปลงที่ย้อนกลับจึงไม่มีวันถูกส่งออกไป

## API key

| เรื่อง | กติกา |
|---|---|
| รูปแบบ | `hp_<prefix 10 ตัว>_<ความลับ 256 bit>` แสดงครั้งเดียว |
| การเก็บ | prefix และ SHA-256 ของ key ทั้งก้อน ไม่มีคอลัมน์ใดเก็บ key เทียบแบบเวลาคงที่ |
| scope | เฉพาะที่ผู้สร้างถือ และไม่ใช่ scope ระดับ 2FA, `integration:manage`, `staff:manage` หรือ wildcard (`Domain/Access/SecondFactorScopes.cs`) |
| ตรวจทุกคำขอ | บัญชีและสมาชิกพนักงานของผู้สร้างยังใช้งานอยู่ scope ที่ใช้ได้คือ scope ของ key ที่ผู้สร้างยังถือ **ตอนนี้** สาขาตัดด้วยสาขาปัจจุบันของผู้สร้าง ถ้าไม่เหลือสาขาเลย key ถูกปฏิเสธ |
| อายุ | บังคับ ค่าเริ่มต้น 90 วัน สูงสุด 365 วัน |
| ขอบเขต | ใช้ได้เฉพาะ endpoint ที่ประกาศ `RequireScope` เส้นของคน (`/me`, เซสชัน ฟีดปฏิทิน) ได้ `403` และ `POST /auth/logout` แบบทุกอุปกรณ์ไม่เพิกถอนเซสชันของผู้สร้าง |
| 2FA | key ไม่มีปัจจัยที่สอง จึงถือ scope ที่ต้องใช้ 2FA ไม่ได้เลย แทนที่จะได้รับการยกเว้น |
| rate limit | ถังของตัวเองต่อ key (`RateLimits:PerApiKeyPerMinute`) แยกจากผู้สร้าง |
| ยกเลิก | `DELETE /staff/api-keys/{id}` ตั้ง `revoked_at` คำขอถัดไปของ key ได้ `401` เพราะการยืนยันตัวอ่านแถวจากฐานข้อมูลทุกคำขอและไม่มี cache ไม่ใช้ `If-Match` เพื่อไม่ให้การยกเลิกชนกับ `last_used_at` ที่ key เขียนทุกนาที |
| รายการ | `GET /staff/api-keys` มี prefix scope สาขา สถานะ และเวลา ไม่มีตัว key หรือ hash ผู้ที่ผูกสาขาเห็นและยกเลิกได้เฉพาะ key ที่ทุกสาขาเป็นของตัวเอง |
| audit | การออก key บันทึก `integration.api_key_created` การยกเลิกบันทึก `integration.api_key_revoked` ทุกแถว audit ที่เกิดจาก key มี `via_api_key` ใน `details` |

**key ที่หลุดหยุดได้ทันที** ด้วยปุ่มยกเลิก key ในหน้า API key ([contract-extensions.md § 22](../contract-extensions.md)) ไม่ต้องปิดบัญชีผู้สร้างหรือแก้ role ของเขาอีก

## ฟีดปฏิทิน

- token ในลิงก์ (`hpcal_` และ 256 bit) เป็น credential ฐานข้อมูลเก็บ SHA-256 ไว้ค้นหา และ ciphertext ไว้แสดงลิงก์เดิม
- token ให้สิทธิ์อ่านฟีดของเจ้าของเท่านั้น และถูกตรวจกับสถานะของเจ้าของทุกครั้ง
- ฟีดไม่มีอาการ เหตุผลของนัด ผลวินิจฉัย หรือข้อมูลติดต่อของเจ้าของ ตอบด้วย `Cache-Control: private, no-store`
- token อยู่ใน URL จึงไปอยู่ใน log ของ proxy และประวัติของแอปปฏิทินได้ เป็นข้อแลกเปลี่ยนที่ฟีดปฏิทินทุกเจ้ามี ลิงก์ที่หลุดหยุดได้ด้วยปุ่ม "สร้างลิงก์ใหม่" (`POST /staff/integrations/calendar/token`) ซึ่งแทนทั้ง hash และ ciphertext ลิงก์เดิมได้ `401` ทันที เจ้าของทำได้กับลิงก์ของตัวเองเท่านั้น API key ทำไม่ได้

## token ของจอห้องรอ

ทีวีในห้องรอเปิดคิวได้โดยไม่ถือเซสชันพนักงาน ([contract-extensions.md § 30](../contract-extensions.md)) เป็นเครื่องที่ตั้งอยู่ในที่สาธารณะ ไม่มีคนเฝ้า จึงได้ credential ที่เล็กที่สุดเท่าที่ทำได้

| เรื่อง | กติกา |
|---|---|
| ความลับสองชั้น | รหัสตั้งจอ 8 ตัว แสดงครั้งเดียวใน console ใช้ได้ครั้งเดียวภายใน 30 นาที แลกเป็น token (`hpdsp_` และ 256 bit) ซึ่งตอบครั้งเดียวให้เซิร์ฟเวอร์ Next.js ของทีวี คนไม่เคยเห็น token |
| การเก็บ | SHA-256 ของรหัสและของ token ใน `queue_displays` ไม่มีคอลัมน์ใดเก็บค่าจริง รายการจอไม่มีทั้งสองอย่างและไม่มี hash การแลกเป็น UPDATE เดียวที่มีเงื่อนไขว่ารหัสยังเป็นค่านี้และยังไม่มี token สองเครื่องที่พิมพ์รหัสเดียวกันพร้อมกันได้ token เครื่องเดียว |
| ใช้ได้ที่ไหน | `GET /queue/display-board` และ `GET /queue/stream` ของสาขาของจอเท่านั้น สองเส้นนี้ติด `[AcceptsDisplayToken]` เส้นอื่นทุกเส้นได้ `401` จาก `DisplayTokenAuthenticationHandler` ก่อนถึง policy ใด ๆ และ `InteractiveCallerRequirement` ปฏิเสธซ้ำอีกชั้น บนเส้นสาธารณะ token ไม่ถูกนับ มีเทสต์ลองสิบเส้น |
| principal | claim `dsp` และ `brn` เท่านั้น ไม่มี `sub` สมาชิกพนักงาน ลูกค้า scope หรือ 2FA |
| ข้อมูลที่ออกไป | เลขคิว สถานะ (`waiting`, `called`, `in_room`) ห้อง ชื่อหมอ เวลาเรียก จำนวนที่รอ และห้องตรวจของสาขา ไม่มีรหัสหรือชื่อของสัตว์และเจ้าของ บริการ ความเร่งด่วน หรือรหัสบัตร stream ส่งให้จอแค่ `branch_id` |
| ตรวจทุกคำขอ | อ่านแถวทุกคำขอ ไม่มี cache stream ที่เปิดอยู่ถูกอ่านซ้ำทุก 20 วินาที |
| ยกเลิก | `DELETE /staff/queue/displays/{id}` (`queue:write` ในสาขาของตัวเอง) คำขอถัดไปได้ `401` stream ปิดภายใน 20 วินาที รหัสที่ยังไม่ใช้ใช้ไม่ได้ทันที |
| อายุ | ไม่บังคับ ไม่เกิน 2 ปี ไม่ตั้งคือจนกว่าจะยกเลิก |
| rate limit | การแลกรหัส 30 ครั้งต่อชั่วโมงต่อ IP คำขอของจอ 120 ครั้งต่อนาทีต่อจอ ดู [การป้องกันระดับคำขอ](request-protection.md) |
| audit | `queue.display_created`, `queue.display_claimed` (ไม่มีผู้กระทำ) และ `queue.display_revoked` |

**ฝั่งหน้าเว็บ** token อยู่ในคุกกี้ `hp_display` แบบ httpOnly `Path=/display` script บนหน้าอ่านไม่ได้ และเบราว์เซอร์ส่งไปแค่ `/display`, `/display/setup` และ `/display/stream` รหัสตั้งจอที่มากับลิงก์อยู่หลัง `#` เบราว์เซอร์ไม่ส่งส่วนนี้ไปเซิร์ฟเวอร์ และหน้าตั้งจอลบออกจากแถบที่อยู่ก่อนส่ง รหัสที่อาจค้างในประวัติของเบราว์เซอร์ใช้ไม่ได้แล้วหลังใช้ครั้งแรกหรือหลัง 30 นาที token ไม่เคยอยู่ใน URL ใดเลย

**ความเสี่ยงที่เหลือ** คนที่คัดลอกคุกกี้จากทีวีไปได้ เปิดจอของสาขานั้นที่อื่นได้จนกว่าจะถูกยกเลิก ซึ่งเห็นแค่สิ่งที่จอในห้องรอแสดงต่อทุกคนอยู่แล้ว token ไม่ผูกกับ IP หรืออุปกรณ์ และไม่หมดอายุเองถ้าไม่ได้ตั้ง จอที่เลิกใช้จึงต้องกดยกเลิก

## ค่าตั้งของผู้ให้บริการ

- ความลับ (`secret_key`, `webhook_secret`, `api_key`) เขียนได้อย่างเดียว เข้ารหัสก่อนเก็บ ไม่มี endpoint คืนค่า audit บันทึกแค่ว่าช่องไหนเปลี่ยน
- ไม่มีตัวเชื่อม gateway หรือแล็บ ค่าตั้งจึงยังไม่ถูกใช้เรียกที่ไหนเลย

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| กติกาที่อยู่และ URL | `backend/src/HappyPaws.Domain/Integrations/WebhookAddressRules.cs`, `WebhookUrl.cs` |
| ตัวส่งและการตรวจตอนส่ง | `backend/src/HappyPaws.Infrastructure/Webhooks/` |
| ลายเซ็น | `backend/src/HappyPaws.Application/Integrations/Webhooks/WebhookSignature.cs` |
| API key | `backend/src/HappyPaws.Domain/Integrations/ApiKey.cs`, `backend/src/HappyPaws.Application/Integrations/ApiKeys/ApiKeyAuthenticator.cs`, `ApiKeyReach.cs`, `RevokeApiKey/`, `backend/src/HappyPaws.Api/Authorization/ApiKeyAuthenticationHandler.cs`, `InteractiveCallerRequirement.cs` |
| ปิด ลบ และหมุน secret ของ webhook | `backend/src/HappyPaws.Domain/Integrations/WebhookSubscription.cs`, `backend/src/HappyPaws.Application/Integrations/UpdateWebhookSubscription/`, `DeleteWebhookSubscription/`, `RotateWebhookSecret/`, `DispatchWebhooks/DispatchWebhooksHandler.cs` |
| token ของจอห้องรอ | `backend/src/HappyPaws.Domain/Queueing/QueueDisplay.cs`, `backend/src/HappyPaws.Application/Queueing/Displays/`, `backend/src/HappyPaws.Api/Authorization/DisplayTokenAuthenticationHandler.cs`, `AcceptsDisplayTokenAttribute.cs`, `InteractiveCallerRequirement.cs`, `frontend/src/lib/display/display-session.ts` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Integrations/` (รวม `WebhookManagementTests.cs`, `ApiKeyManagementTests.cs`, `CalendarTokenTests.cs`), `backend/tests/HappyPaws.IntegrationTests/Api/Queueing/QueueDisplayTests.cs`, `DisplayClaimLimitTests.cs`, `backend/tests/HappyPaws.IntegrationTests/Webhooks/WebhookHttpHandlerTests.cs`, `backend/tests/HappyPaws.UnitTests/Domain/Integrations/` |

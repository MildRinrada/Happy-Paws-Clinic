# การเชื่อมต่อระบบภายนอก

webhook ขาออก API key ฟีดปฏิทิน และค่าตั้งของผู้ให้บริการ เป็นทางที่ข้อมูลออกจากระบบ หรือที่เครื่องอื่นเข้ามาโดยไม่มีคนเข้าสู่ระบบ หน้านี้บอกว่าแต่ละทางกันอะไรไว้ การทำงานทั้งหมดอยู่ใน [การเชื่อมต่อระบบ](../features/integrations.md)

## SSRF ของ webhook

URL ของ webhook มาจากคนพิมพ์ แต่เซิร์ฟเวอร์เป็นผู้เรียกจากในเครือข่ายของคลินิก ถ้าไม่กัน ใครที่มี `integration:manage` ก็สั่งให้เซิร์ฟเวอร์ยิงเข้าฐานข้อมูล เราเตอร์ หรือบริการ metadata ของ cloud (`169.254.169.254`) ซึ่งคืน credential ของเครื่องได้

| ชั้น | ที่ไหน | ทำอะไร |
|---|---|---|
| ตอนสร้าง | `Domain/Integrations/WebhookUrl.cs` | บังคับ `https` ไม่มี user info ไม่มี fragment ไม่รับ `localhost` ชื่อที่ลงท้าย `.local`, `.localhost`, `.internal` และ IP ที่ไม่ใช่สาธารณะ |
| ก่อนส่ง | `Infrastructure/Webhooks/WebhookAddressGuard.cs` | resolve ชื่อ ถ้าที่อยู่ **ใดก็ได้** ไม่ใช่สาธารณะ ไม่ส่ง (`address_blocked`) |
| ตอนต่อ socket | `Infrastructure/Webhooks/WebhookHttpHandler.cs` | connect callback resolve และตรวจซ้ำ แล้วต่อไปยังที่อยู่ที่เพิ่งตรวจ กัน DNS rebinding |
| คำตอบ | `WebhookHttpHandler.cs`, `WebhookHttpSender.cs` | ไม่ตาม redirect ไม่ใช้ proxy ไม่ใช้คุกกี้ timeout ทั้งครั้ง อ่านคำตอบไม่เกิน 4 KB เก็บ 1,000 ตัวอักษร |

กติกาของที่อยู่อยู่ใน `Domain/Integrations/WebhookAddressRules.cs` ที่เดียว ปฏิเสธ loopback, `0/8`, `10/8`, `100.64/10`, `127/8`, `169.254/16`, `172.16/12`, `192.0.0/24`, `192.168/16`, `198.18/15`, multicast และช่วงสงวน ฝั่ง IPv6 ปฏิเสธ `::`, `::1`, `fc00::/7`, `fe80::/10`, `fec0::/10`, multicast และตรวจ IPv4 ที่ซ่อนอยู่ใน `::ffff:0:0/96`, `::/96` และ `64:ff9b::/96` ตามกติกา IPv4

Development ยอม `http://localhost` และ loopback (ปิดได้ด้วย `Webhooks:AllowLocalhostInDevelopment`) ช่วงส่วนตัวและ metadata ปฏิเสธทุก environment ไม่มีค่าตั้งที่เปิดได้

URL ของ API แล็บ (`PUT /staff/integrations/lab`) ใช้กติกาตอนสร้างชุดเดียวกัน เพราะตัวเชื่อมในอนาคตจะเรียกจากในเครือข่ายเช่นกัน

## ลายเซ็น webhook

- HMAC-SHA256 ด้วย secret ของ webhook บน `<timestamp>.<body>` ส่งใน `X-Signature: sha256=<hex>` พร้อม `X-Webhook-Timestamp` การลงนาม timestamp ด้วยทำให้ผู้รับปฏิเสธคำขอที่ถูกดักแล้วส่งซ้ำได้
- secret สุ่ม 256 bit สร้างฝั่งเซิร์ฟเวอร์ (`whsec_...`) แสดงครั้งเดียวตอนสร้าง คำตอบมี `Cache-Control: no-store`
- secret ต้องอ่านกลับได้เพื่อใช้ลงนาม จึงเก็บแบบเข้ารหัสด้วย Data Protection ไม่ใช่ hash ([ข้อมูลและความลับ](data-and-secrets.md#ความลับที่เข้ารหัสด้วย-data-protection))
- ไม่มีการส่งแบบไม่ลงนาม ถ้าถอดรหัส secret ไม่ได้ การส่งล้มเหลวด้วย `secret_unavailable`

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
| audit | การออก key บันทึก `integration.api_key_created` ทุกแถว audit ที่เกิดจาก key มี `via_api_key` ใน `details` |

**ยังยกเลิก key ก่อนหมดอายุไม่ได้** contract ไม่มี endpoint นี้ คอลัมน์ `revoked_at` และการตรวจตอนยืนยันตัวมีแล้ว ระหว่างนี้วิธีหยุด key ทันทีคือปิดบัญชีผู้สร้าง หรือเอา scope ออกจาก role ของผู้สร้าง ข้อเสนอ `DELETE /staff/api-keys/{id}` อยู่ใน [การเชื่อมต่อระบบ](../features/integrations.md#ข้อเสนอส่วนขยาย-ยังไม่ได้ขออนุมัติ-ยังไม่ได้ทำ)

## ฟีดปฏิทิน

- token ในลิงก์ (`hpcal_` และ 256 bit) เป็น credential ฐานข้อมูลเก็บ SHA-256 ไว้ค้นหา และ ciphertext ไว้แสดงลิงก์เดิม
- token ให้สิทธิ์อ่านฟีดของเจ้าของเท่านั้น และถูกตรวจกับสถานะของเจ้าของทุกครั้ง
- ฟีดไม่มีอาการ เหตุผลของนัด ผลวินิจฉัย หรือข้อมูลติดต่อของเจ้าของ ตอบด้วย `Cache-Control: private, no-store`
- token อยู่ใน URL จึงไปอยู่ใน log ของ proxy และประวัติของแอปปฏิทินได้ เป็นข้อแลกเปลี่ยนที่ฟีดปฏิทินทุกเจ้ามี ลิงก์ที่หลุดใช้ได้จนกว่าเจ้าของถูกปิดบัญชี เพราะยังไม่มีทางสร้างลิงก์ใหม่

## ค่าตั้งของผู้ให้บริการ

- ความลับ (`secret_key`, `webhook_secret`, `api_key`) เขียนได้อย่างเดียว เข้ารหัสก่อนเก็บ ไม่มี endpoint คืนค่า audit บันทึกแค่ว่าช่องไหนเปลี่ยน
- ไม่มีตัวเชื่อม gateway หรือแล็บ ค่าตั้งจึงยังไม่ถูกใช้เรียกที่ไหนเลย

## โค้ด

| ส่วน | ไฟล์ |
|---|---|
| กติกาที่อยู่และ URL | `backend/src/HappyPaws.Domain/Integrations/WebhookAddressRules.cs`, `WebhookUrl.cs` |
| ตัวส่งและการตรวจตอนส่ง | `backend/src/HappyPaws.Infrastructure/Webhooks/` |
| ลายเซ็น | `backend/src/HappyPaws.Application/Integrations/Webhooks/WebhookSignature.cs` |
| API key | `backend/src/HappyPaws.Domain/Integrations/ApiKey.cs`, `backend/src/HappyPaws.Application/Integrations/ApiKeys/ApiKeyAuthenticator.cs`, `backend/src/HappyPaws.Api/Authorization/ApiKeyAuthenticationHandler.cs`, `InteractiveCallerRequirement.cs` |
| เทสต์ | `backend/tests/HappyPaws.IntegrationTests/Api/Integrations/`, `backend/tests/HappyPaws.IntegrationTests/Webhooks/WebhookHttpHandlerTests.cs`, `backend/tests/HappyPaws.UnitTests/Domain/Integrations/` |

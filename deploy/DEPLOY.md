# نشر ShopEx على سيرفر العميل (IIS + SQL Server على نفس السيرفر، HTTPS)

محتويات الباكدج (`deploy\out`):
- `app` : الـ API + الواجهة (داخل wwwroot). موقع IIS واحد فقط. **لأول نشر.**
- `update` : نفس الملفات بدون web.config / appsettings.json. **للتحديثات بعد كده.**
- `shopex.sql` : سكريبت قاعدة `ShopEXdb` (idempotent، آمن لو اتشغل أكتر من مرة).
- `cert\shopex.pfx` + `cert\shopex.cer` : شهادة HTTPS لـ `82.129.144.54` (صالحة لحد 2031).

## متطلبات السيرفر (مرة واحدة)
- IIS + **ASP.NET Core Hosting Bundle 9** (بعد تثبيته: `iisreset`).
- SQL Server وعليه قاعدة الـ RMS: `HQDB_ELJOKER`.

## أول نشر
1. **قاعدة البيانات**: من SSMS أنشئ قاعدة فاضية اسمها `ShopEXdb`، وشغّل عليها `shopex.sql`.
2. **الملفات**: انسخ فولدر `app` إلى `C:\inetpub\ShopEx`.
3. **الشهادة على السيرفر**: دبل كليك على `shopex.pfx` -> Local Machine -> (باسورد الـ pfx) -> Personal.
4. **موقع IIS**: Add Website
   - Physical path: `C:\inetpub\ShopEx`
   - Binding: **https**، Port 443، IP: All Unassigned، SSL certificate: `82.129.144.54`.
   - Application Pool: **No Managed Code** (افتراضيا اسمه نفس اسم الموقع، مثلا `ShopEx`).
   - (اختياري) امسح binding الـ http على 80 أو سيبه، الـ API بيحوّل لـ https تلقائيا.
   - افتح Port 443 في Windows Firewall (وعلى الراوتر لو الدخول من برّه الشبكة).
5. **صلاحيات SQL** (Windows auth): في SSMS نفّذ (غيّر `ShopEx` لاسم الـ App Pool لو مختلف):
   ```sql
   CREATE LOGIN [IIS APPPOOL\ShopEx] FROM WINDOWS;
   USE ShopEXdb;     CREATE USER [IIS APPPOOL\ShopEx]; ALTER ROLE db_owner ADD MEMBER [IIS APPPOOL\ShopEx];
   USE HQDB_ELJOKER; CREATE USER [IIS APPPOOL\ShopEx]; ALTER ROLE db_datareader ADD MEMBER [IIS APPPOOL\ShopEx];
                                                       ALTER ROLE db_datawriter ADD MEMBER [IIS APPPOOL\ShopEx];
   ```
   (`db_datawriter` على الـ RMS مطلوب لأن الـ Worksheets بتتكتب هناك.)
6. **`appsettings.json`** (في `C:\inetpub\ShopEx`): الـ connection strings جاهزة على `Server=.`
   و `HQDB_ELJOKER`. عدّلها بس لو الـ SQL instance مش الـ default (مثلا `Server=.\SQLEXPRESS`).
7. **الإعدادات السرية** في `web.config` داخل `<aspNetCore ...>`:
   ```xml
   <environmentVariables>
     <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
     <environmentVariable name="Jwt__Key" value="(نص عشوائي 48+ حرف)" />
     <environmentVariable name="InitialAdmin__Username" value="admin" />
     <environmentVariable name="InitialAdmin__Password" value="(باسورد قوي)" />
     <environmentVariable name="InitialDev__Username" value="(حساب المطوّر)" />
     <environmentVariable name="InitialDev__Password" value="(باسورد المطوّر)" />
   </environmentVariables>
   ```
   - مفتاح JWT عشوائي من PowerShell:
     `[Convert]::ToBase64String((1..48 | % { Get-Random -Max 256 }) -as [byte[]])`
   - `License__InitialGraceDays` (اختياري، الافتراضي 30 يوم) = مدة أول اشتراك.
8. شغّل الموقع وافتح `https://82.129.144.54/` وسجّل دخول بالأدمن.
9. **بعد أول تشغيل ناجح**: امسح من `web.config` سطور `InitialAdmin__Password` و `InitialDev__Password`
   (الحسابات اتعملت خلاص)، واعمل Recycle للـ App Pool.

## أجهزة المستخدمين (مرة لكل جهاز)
الشهادة self-signed، فكل جهاز لازم يثق فيها وإلا المتصفح هيظهر تحذير:
دبل كليك على `shopex.cer` -> Install Certificate -> Local Machine ->
"Place all certificates in the following store" -> **Trusted Root Certification Authorities**. ثم أعد فتح المتصفح.

## لو حصلت مشكلة
- 500.19 / 500.30 / 502.5: غالبا الـ Hosting Bundle ناقص أو `Jwt__Key` مش مضبوط. اللوج في `C:\inetpub\ShopEx\logs\stdout*`.
- خطأ اتصال DB / Login failed: راجع خطوة 5 (اسم الـ App Pool لازم يطابق).
- `LICENSE_EXPIRED`: ادخل `https://82.129.144.54/dev/login` بحساب المطوّر ومدّد الاشتراك.

---
## تحديث نسخة موجودة (بعد أول نشر)
استخدم فولدر `update` (مش `app`): ما فيهوش web.config ولا appsettings.json فمش هيمسح إعدادات السيرفر.
1. اعمل Backup لقاعدة `ShopEXdb` من SSMS.
2. في فولدر الموقع اعمل ملف فاضي اسمه `app_offline.htm` (بيوقف الموقع مؤقتا وبيفك قفل الملفات).
3. انسخ محتوى `update` فوق فولدر الموقع (Replace all).
4. شغّل `shopex.sql` على `ShopEXdb` (بينفّذ الجديد بس).
5. لو فيه إعدادات جديدة ضيفها في web.config.
6. امسح `app_offline.htm`. افتح الموقع واعمل Ctrl+F5.

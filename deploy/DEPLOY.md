# نشر ShopEx على سيرفر العميل (IIS، كل شيء على نفس السيرفر)

الفولدر `app` فيه الـ API + الواجهة (داخل wwwroot). موقع IIS واحد فقط.

## متطلبات السيرفر (مرة واحدة)
- IIS + **ASP.NET Core Hosting Bundle 9** (بعد تثبيته: `iisreset`)
- SQL Server وعليه قاعدة `RMSHQ`

## الخطوات
1. أنشئ قاعدة فاضية اسمها `ShopEXdb`، ثم شغّل `shopex.sql` عليها (SSMS).
2. انسخ فولدر `app` إلى `C:\inetpub\ShopEx`.
3. IIS: Add Website -> المسار `C:\inetpub\ShopEx`، البورت مثلا 80. الـ Application Pool: **No Managed Code**.
4. صلاحيات الـ DB: الـ App Pool identity (`IIS APPPOOL\<اسم الـ pool>`) لازم يكون له login في SQL Server
   وصلاحية `db_owner` على `ShopEXdb` وقراءة على `RMSHQ`. (أو غيّر الـ connection strings لـ SQL login.)
5. افتح `C:\inetpub\ShopEx\appsettings.json` وعدّل:
   - `ConnectionStrings` لو الـ SQL Server مش `.` (default instance).
   - `"DisableHttpsRedirection": true` لو التشغيل على HTTP داخل الشبكة بدون شهادة.
6. متغيرات البيئة (IIS -> Configuration Editor -> system.webServer/aspNetCore -> environmentVariables)
   أو في `web.config`:
   - `Jwt__Key` = نص عشوائي طويل 32+ حرف (**إجباري**)
   - `InitialAdmin__Username` / `InitialAdmin__Password` = أول أدمن (يُنشأ فقط لو جدول Users فاضي)
   - `ASPNETCORE_ENVIRONMENT` = `Production`
7. افتح المتصفح على `http://<server>/` وسجّل الدخول.

## لو حصلت مشكلة
- 500.19/502.5: غالبا الـ Hosting Bundle ناقص. اللوج في `logs\stdout*`.
- خطأ اتصال DB: راجع صلاحيات الـ App Pool identity.
- تحديث لاحق: أوقف الموقع، استبدل الملفات (بدون مسح appsettings المعدّل)، شغّل أي migration جديد، شغّل الموقع.

---
## تحديث نسخة موجودة (بعد أول نشر)
استخدم فولدر `update` (مش `app`): ما فيهوش web.config ولا appsettings.json فمش هيمسح إعدادات السيرفر.
1. اعمل Backup لقاعدة `ShopEXdb` من SSMS.
2. في فولدر الموقع اعمل ملف فاضي اسمه `app_offline.htm` (بيوقف الموقع مؤقتا وبيفك قفل الملفات).
3. انسخ محتوى `update` فوق فولدر الموقع (Replace all).
4. شغّل `shopex.sql` على `ShopEXdb` (idempotent: بينفّذ الجديد بس، وآمن لو اتشغل تاني).
5. لو فيه إعدادات جديدة (زي InitialDev__Username/Password) ضيفها في web.config.
6. امسح `app_offline.htm`. افتح الموقع واعمل Ctrl+F5.

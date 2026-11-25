import { Injectable } from '@angular/core';
import { DeviceResponse, MaintenanceTicket } from '../../models/models';

/**
 * فاتورة الصيانة على طابعة حرارية (Thermal Receipt) بعرض 80mm.
 * تُطبع من iframe مخفي مستقل (بدون التأثير على أي ستايل للصفحة أو على طباعة A4)،
 * وبدون نوافذ منبثقة فلا يحجبها الـ popup blocker حتى لو استُدعيت بعد استجابة من السيرفر.
 */
@Injectable({ providedIn: 'root' })
export class ThermalReceiptService {
  /** عرض الورق بالمليمتر (80 أو 58). الطابعات الحرارية الشائعة 80mm */
  paperWidthMm = 80;

  printMaintenance(ticket: MaintenanceTicket): void {
    this.printHtml(this.buildMaintenanceHtml(ticket));
  }

  /** فاتورة بيع جهاز للعميل (سعر البيع فقط: لا تظهر التكلفة ولا الربح) */
  printDeviceSale(d: DeviceResponse): void {
    this.printHtml(this.buildDeviceSaleHtml(d));
  }

  // ------------------------------------------------------------------

  private printHtml(html: string): void {
    const iframe = document.createElement('iframe');
    iframe.setAttribute('aria-hidden', 'true');
    iframe.style.cssText = 'position:fixed;right:0;bottom:0;width:0;height:0;border:0;visibility:hidden;';
    document.body.appendChild(iframe);

    const win = iframe.contentWindow;
    const doc = iframe.contentDocument ?? win?.document;
    if (!win || !doc) {
      iframe.remove();
      return;
    }

    let cleaned = false;
    const cleanup = () => {
      if (cleaned) return;
      cleaned = true;
      iframe.remove();
    };

    doc.open();
    doc.write(html);
    doc.close();

    // نمهل المتصفح لحظة لعرض المحتوى قبل فتح نافذة الطباعة
    setTimeout(() => {
      win.addEventListener('afterprint', cleanup);
      win.focus();
      win.print();
      // احتياطاً لو لم يُطلق afterprint في بعض المتصفحات
      setTimeout(cleanup, 60_000);
    }, 250);
  }

  private buildMaintenanceHtml(t: MaintenanceTicket): string {
    const w = this.paperWidthMm;
    const bodyWidth = w - 8; // هامش 4mm من كل جانب
    const esc = (v: unknown) =>
      String(v ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
    const money = (n: number) =>
      (Number(n) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const pad = (n: number) => n.toString().padStart(2, '0');
    const fmtDate = (iso?: string | null) => {
      const d = iso ? new Date(iso) : new Date();
      return `${d.getFullYear()}/${pad(d.getMonth() + 1)}/${pad(d.getDate())}  ${pad(d.getHours())}:${pad(d.getMinutes())}`;
    };

    const device = [t.deviceName, t.brand, t.model].filter((x, i, a) => !!x && a.indexOf(x) === i).join(' ');

    const operations = (t.operations || [])
      .map(
        (op) => `
        <tr>
          <td class="op">${esc(op.operationName)}${op.notes ? `<div class="sm">${esc(op.notes)}</div>` : ''}</td>
          <td class="amt">${money(op.cost)}</td>
        </tr>`
      )
      .join('');

    const remaining = Math.max(0, t.remainingAmount ?? 0);

    return `<!doctype html>
<html lang="ar" dir="rtl">
<head>
<meta charset="utf-8">
<title>فاتورة صيانة ${esc(t.ticketNumber)}</title>
<style>${this.css(w, bodyWidth)}</style>
</head>
<body>
  <div class="c">
    <div class="brand">ShopEx</div>
    ${t.storeName ? `<div class="sm">${esc(t.storeName)}</div>` : ''}
    <div class="title">فاتورة صيانة</div>
  </div>

  <hr class="hr">

  <div class="row"><b>رقم التذكرة:</b><span>${esc(t.ticketNumber)}</span></div>
  <div class="row"><b>التاريخ:</b><span dir="ltr">${fmtDate(t.deliveredAt)}</span></div>
  <div class="row"><b>العميل:</b><span>${esc(t.customerName)}</span></div>
  ${t.customerPhone ? `<div class="row"><b>الهاتف:</b><span dir="ltr">${esc(t.customerPhone)}</span></div>` : ''}
  <div class="row"><b>الجهاز:</b><span>${esc(device)}</span></div>

  <hr class="hr">

  <table>
    <thead>
      <tr><th>الإصلاحات / البنود</th><th class="amt">التكلفة</th></tr>
    </thead>
    <tbody>
      ${operations || '<tr><td class="op" colspan="2">—</td></tr>'}
    </tbody>
  </table>

  <hr class="hr">

  <div class="totals">
    <div class="row grand"><b>الإجمالي</b><span dir="ltr">${money(t.totalCost)} ج.م</span></div>
    <div class="row"><b>المدفوع</b><span dir="ltr">${money(t.paidAmount)} ج.م</span></div>
    ${remaining > 0 ? `<div class="row"><b>المتبقي</b><span dir="ltr">${money(remaining)} ج.م</span></div>` : ''}
  </div>

  ${t.warrantyNote ? `<div class="warranty">${esc(t.warrantyNote)}</div>` : ''}

  <hr class="hr">
  <div class="foot">شكراً لثقتكم بنا</div>
</body>
</html>`;
  }

  private css(w: number, bodyWidth: number): string {
    return `
  @page { size: ${w}mm auto; margin: 0; }
  * { box-sizing: border-box; }
  html, body { margin: 0; padding: 0; background: #fff; }
  body {
    width: ${w}mm;
    padding: 4mm;
    font-family: Tahoma, 'Segoe UI', Arial, sans-serif;
    font-size: 12px;
    line-height: 1.45;
    color: #000;
  }
  .c { text-align: center; }
  .brand { font-size: 20px; font-weight: 800; letter-spacing: .5px; }
  .title { font-size: 14px; font-weight: 700; margin-top: 2px; }
  .sm { font-size: 10px; color: #000; }
  .hr { border: 0; border-top: 1px dashed #000; margin: 6px 0; }
  .row { display: flex; justify-content: space-between; gap: 6px; margin: 1px 0; }
  .row b { white-space: nowrap; }
  .row span { text-align: left; word-break: break-word; }
  table { width: 100%; border-collapse: collapse; width: ${bodyWidth}mm; }
  th { text-align: right; font-size: 11px; border-bottom: 1px solid #000; padding: 2px 0; }
  th.amt, td.amt { text-align: left; white-space: nowrap; padding-left: 0; }
  td { padding: 3px 0; vertical-align: top; border-bottom: 1px dotted #999; }
  td.op { word-break: break-word; padding-left: 6px; }
  .totals .row { font-size: 12px; }
  .grand { font-size: 15px; font-weight: 800; }
  .warranty { border: 1px solid #000; padding: 4px 6px; margin-top: 6px; text-align: center; font-weight: 700; }
  .foot { margin-top: 8px; text-align: center; font-size: 11px; }
`;
  }

  private buildDeviceSaleHtml(d: DeviceResponse): string {
    const w = this.paperWidthMm;
    const bodyWidth = w - 8;
    const esc = (v: unknown) =>
      String(v ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
    const money = (n: number) =>
      (Number(n) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const pad = (n: number) => n.toString().padStart(2, '0');
    const fmtDate = (iso?: string | null) => {
      const x = iso ? new Date(iso) : new Date();
      return `${x.getFullYear()}/${pad(x.getMonth() + 1)}/${pad(x.getDate())}  ${pad(x.getHours())}:${pad(x.getMinutes())}`;
    };

    const name = `${d.brand} ${d.model}`.trim();

    return `<!doctype html>
<html lang="ar" dir="rtl">
<head>
<meta charset="utf-8">
<title>فاتورة بيع ${esc(name)}</title>
<style>${this.css(w, bodyWidth)}</style>
</head>
<body>
  <div class="c">
    <div class="brand">ShopEx</div>
    ${d.soldStoreName ? `<div class="sm">${esc(d.soldStoreName)}</div>` : ''}
    <div class="title">فاتورة بيع جهاز</div>
  </div>

  <hr class="hr">

  <div class="row"><b>رقم العملية:</b><span dir="ltr">DV-${d.id}</span></div>
  <div class="row"><b>التاريخ:</b><span dir="ltr">${fmtDate(d.soldAt)}</span></div>
  ${d.buyerName ? `<div class="row"><b>العميل:</b><span>${esc(d.buyerName)}</span></div>` : ''}
  ${d.buyerPhone ? `<div class="row"><b>الهاتف:</b><span dir="ltr">${esc(d.buyerPhone)}</span></div>` : ''}

  <hr class="hr">

  <div class="row"><b>الجهاز:</b><span>${esc(name)}</span></div>
  ${d.storage ? `<div class="row"><b>السعة:</b><span dir="ltr">${esc(d.storage)}</span></div>` : ''}
  ${d.color ? `<div class="row"><b>اللون:</b><span>${esc(d.color)}</span></div>` : ''}
  ${d.imei ? `<div class="row"><b>IMEI:</b><span dir="ltr">${esc(d.imei)}</span></div>` : ''}
  ${d.batteryHealth != null ? `<div class="row"><b>البطارية:</b><span dir="ltr">${d.batteryHealth}%</span></div>` : ''}
  <div class="row"><b>العلبة:</b><span>${d.hasBox ? 'معه العلبة' : 'بدون علبة'}</span></div>

  <hr class="hr">

  <div class="totals">
    <div class="row grand"><b>السعر</b><span dir="ltr">${money(d.salePrice ?? 0)} ج.م</span></div>
  </div>

  <hr class="hr">
  <div class="foot">شكراً لثقتكم بنا</div>
</body>
</html>`;
  }
}

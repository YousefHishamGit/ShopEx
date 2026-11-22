import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrintSheetComponent } from './print-sheet.component';
import { CustomerInvoiceDetail } from '../../models/models';

/** Official printout of a customer's open invoice (RMS). */
@Component({
  selector: 'app-customer-invoice-print',
  standalone: true,
  imports: [CommonModule, PrintSheetComponent],
  template: `
    <app-print-sheet *ngIf="invoice"
      title="فاتورة بيع"
      [docNumber]="'#' + invoice.invoiceID"
      [status]="invoice.remainingAmount > 0 ? 'غير مسددة بالكامل' : 'مسددة بالكامل'"
      [signatures]="['المستلم / العميل', 'المحاسب', 'الاعتماد والختم']">

      <div class="ps-row">
        <div class="ps-box">
          <div class="ps-box-title">بيانات العميل</div>
          <div class="ps-box-body">
            <div class="ps-kv"><span>الاسم:</span><strong>{{ invoice.customerName || '—' }}</strong></div>
            <div class="ps-kv"><span>رقم الحساب:</span><strong class="ps-mono">{{ invoice.accountNumber || '—' }}</strong></div>
            <div class="ps-kv"><span>الهاتف:</span><strong>{{ invoice.phoneNumber || '—' }}</strong></div>
          </div>
        </div>
        <div class="ps-box">
          <div class="ps-box-title">بيانات الفاتورة</div>
          <div class="ps-box-body">
            <div class="ps-kv"><span>الفرع:</span><strong>{{ invoice.storeName || 'الفرع الرئيسي' }}</strong></div>
            <div class="ps-kv"><span>التاريخ:</span><strong class="ps-mono">{{ invoice.invoiceDate | date:'yyyy/MM/dd hh:mm a' }}</strong></div>
            <div class="ps-kv"><span>مدة الفتح:</span><strong>{{ invoice.daysOld }} يوم</strong></div>
          </div>
        </div>
      </div>

      <table class="ps-table" style="margin-top:12px">
        <thead>
          <tr>
            <th class="ps-c" style="width:32px">#</th>
            <th style="width:90px">الكود</th>
            <th>الصنف</th>
            <th class="ps-c" style="width:60px">الكمية</th>
            <th class="ps-n" style="width:90px">سعر الوحدة</th>
            <th class="ps-n" style="width:100px">الإجمالي</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let item of invoice.items; let i = index">
            <td class="ps-c">{{ i + 1 }}</td>
            <td class="ps-mono">{{ item.itemLookupCode || item.itemID || '—' }}</td>
            <td>{{ item.productName }}</td>
            <td class="ps-c">{{ item.quantity | number:'1.0-2' }}</td>
            <td class="ps-n">{{ item.unitPrice | number:'1.2-2' }}</td>
            <td class="ps-n">{{ item.totalPrice | number:'1.2-2' }}</td>
          </tr>
          <tr *ngIf="invoice.items.length === 0"><td colspan="6" class="ps-c">لا توجد بنود</td></tr>
        </tbody>
      </table>

      <div class="ps-split">
        <div class="ps-note-box" *ngIf="invoice.comment">
          <div class="ps-note-title">ملاحظات</div>
          {{ invoice.comment }}
        </div>
        <div class="ps-totals">
          <table>
            <tr><td>إجمالي الفاتورة</td><td>{{ invoice.totalAmount | number:'1.2-2' }} ج.م</td></tr>
            <tr><td>المسدد (عربون)</td><td>{{ invoice.deposit | number:'1.2-2' }} ج.م</td></tr>
            <tr class="ps-grand"><td>المتبقي</td><td>{{ invoice.remainingAmount | number:'1.2-2' }} ج.م</td></tr>
          </table>
        </div>
      </div>
    </app-print-sheet>
  `
})
export class CustomerInvoicePrintComponent {
  @Input() invoice: CustomerInvoiceDetail | null = null;
}

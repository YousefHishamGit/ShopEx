import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Common A4 letterhead / footer / signatures for every printed document.
 * Styling lives in styles.css (.print-sheet) and is strictly black on white.
 */
@Component({
  selector: 'app-print-sheet',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'print-only' },
  template: `
    <div class="print-sheet" dir="rtl">
      <div class="ps-head">
        <div class="ps-brand">
          <div class="ps-logo">SX</div>
          <div>
            <div class="ps-co">ShopEx</div>
            <div class="ps-sub">{{ subtitle }}</div>
          </div>
        </div>
        <div class="ps-doc">
          <div class="ps-title">{{ title }}</div>
          <div class="ps-number" *ngIf="docNumber">{{ docNumber }}</div>
          <div class="ps-status" *ngIf="status">{{ status }}</div>
        </div>
      </div>

      <ng-content></ng-content>

      <div class="ps-signs" *ngIf="signatures.length">
        <div class="ps-sign" *ngFor="let s of signatures">
          <div class="ps-sign-line">{{ s }}</div>
        </div>
      </div>

      <div class="ps-foot">
        <span>ShopEx — {{ title }}<ng-container *ngIf="docNumber"> رقم {{ docNumber }}</ng-container></span>
        <span>تاريخ الطباعة: {{ now | date:'yyyy/MM/dd hh:mm a' }}</span>
      </div>
    </div>
  `
})
export class PrintSheetComponent {
  @Input() title = '';
  @Input() docNumber?: string | number | null;
  @Input() status?: string | null;
  @Input() subtitle = 'نظام إدارة المحلات والمخازن';
  @Input() signatures: string[] = ['المستلم', 'المحاسب', 'الاعتماد والختم'];

  readonly now = new Date();
}

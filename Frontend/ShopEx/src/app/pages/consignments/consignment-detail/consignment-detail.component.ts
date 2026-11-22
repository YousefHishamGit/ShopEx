import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PrintService } from '../../../shared/print/print.service';
import { PrintSheetComponent } from '../../../shared/print/print-sheet.component';
import { ConsignmentService, PAYMENT_METHODS } from '../../../services/consignment.service';
import {
  Consignment,
  ConsignmentItem,
  CreateConsignmentSettlementRequest,
  SettlementItemRequest,
  AddConsignmentPaymentRequest
} from '../../../models/models';

interface SettlementItemRow {
  consignmentItemID: number;
  rmsItemID: number;
  itemDescription?: string;
  itemLookupCode?: string;
  quantityOutstanding: number;
  unitPrice: number;
  unitCost: number;
  quantitySold: number;
  quantityReturned: number;
}

@Component({
  selector: 'app-consignment-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PrintSheetComponent],
  templateUrl: './consignment-detail.component.html'
})
export class ConsignmentDetailComponent implements OnInit {
  consignmentId!: number;
  consignment: Consignment | null = null;
  loading = true;
  errorMessage = '';

  activeTab: 'items' | 'settlements' | 'payments' = 'items';

  // Settlement Modal
  showSettlementModal = false;
  savingSettlement = false;
  settlementError = '';
  settlementNotes = '';
  settlementCreatedBy = '';
  settlementItems: SettlementItemRow[] = [];
  paymentMethods = PAYMENT_METHODS;
  collectMode: 'now' | 'all' | 'custom' = 'now';
  paymentMode: 'full' | 'half' | 'custom' = 'full';
  collectPaymentNow = false;
  immediatePaymentAmount = 0;
  immediatePaymentMethod = 'نقدي';

  // Payment Modal
  showPaymentModal = false;
  savingPayment = false;
  paymentError = '';
  paymentRequest: AddConsignmentPaymentRequest = {
    amount: 0,
    paymentMethod: 'نقدي',
    notes: ''
  };

  // Cancelling
  cancelling = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private consignmentService: ConsignmentService,
    private printer: PrintService
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.consignmentId = Number(idParam);
      this.loadConsignment();
    }
  }

  loadConsignment(): void {
    this.loading = true;
    this.errorMessage = '';
    this.consignmentService.getConsignment(this.consignmentId).subscribe({
      next: (data) => {
        this.consignment = data;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'حدث خطأ أثناء تحميل بيانات إذن الأمانة.';
        this.loading = false;
      }
    });
  }

  // ----------------------------------------------------
  // Settlement Modal Logic
  // ----------------------------------------------------
  openSettlementModal(): void {
    if (!this.consignment) return;

    this.settlementItems = (this.consignment.items || [])
      .filter((i) => i.quantityOutstanding > 0)
      .map((i) => ({
        consignmentItemID: i.id,
        rmsItemID: i.rmsItemID,
        itemDescription: i.itemDescription,
        itemLookupCode: i.itemLookupCode,
        quantityOutstanding: i.quantityOutstanding,
        unitPrice: i.unitPrice,
        unitCost: i.unitCost,
        quantitySold: 0,
        quantityReturned: 0
      }));

    this.settlementNotes = '';
    this.settlementCreatedBy = '';
    this.collectPaymentNow = false;
    this.collectMode = 'now';
    this.immediatePaymentAmount = 0;
    this.settlementError = '';
    this.showSettlementModal = true;
  }

  closeSettlementModal(): void {
    this.showSettlementModal = false;
  }

  get totalSettlementSoldValue(): number {
    return this.settlementItems.reduce(
      (sum, i) => sum + (Number(i.quantitySold) || 0) * i.unitPrice,
      0
    );
  }

  /** Sell everything that is still outstanding on this line. */
  sellAll(row: SettlementItemRow): void {
    row.quantitySold = row.quantityOutstanding;
    row.quantityReturned = 0;
    this.onSettlementSoldChange();
  }

  /** Return everything that is still outstanding on this line to the warehouse. */
  returnAll(row: SettlementItemRow): void {
    row.quantityReturned = row.quantityOutstanding;
    row.quantitySold = 0;
    this.onSettlementSoldChange();
  }

  /** A line is invalid when its quantities are negative or exceed what is still outstanding. */
  isRowInvalid(row: SettlementItemRow): boolean {
    const sold = Number(row.quantitySold) || 0;
    const ret = Number(row.quantityReturned) || 0;
    return sold < 0 || ret < 0 || sold + ret > row.quantityOutstanding;
  }

  get settlementDueTotal(): number {
    return (this.consignment?.remainingAmount || 0) + this.totalSettlementSoldValue;
  }

  step(row: SettlementItemRow, field: 'quantitySold' | 'quantityReturned', delta: number): void {
    const cur = Number(row[field]) || 0;
    const other = Number(field === 'quantitySold' ? row.quantityReturned : row.quantitySold) || 0;
    row[field] = Math.max(0, Math.min(row.quantityOutstanding - other, cur + delta));
    this.onSettlementSoldChange();
  }

  setCollectMode(mode: 'now' | 'all' | 'custom'): void {
    this.collectMode = mode;
    this.syncPayment();
  }

  setPaymentMode(mode: 'full' | 'half' | 'custom'): void {
    this.paymentMode = mode;
    const remaining = this.consignment?.remainingAmount || 0;
    if (mode === 'full') this.paymentRequest.amount = remaining;
    else if (mode === 'half') this.paymentRequest.amount = Math.round((remaining / 2) * 100) / 100;
  }

  private syncPayment(): void {
    if (!this.collectPaymentNow || this.collectMode === 'custom') return;
    this.immediatePaymentAmount =
      this.collectMode === 'all' ? this.settlementDueTotal : this.totalSettlementSoldValue;
  }

  onSettlementSoldChange(): void {
    this.syncPayment();
  }

  toggleCollectPayment(): void {
    this.syncPayment();
  }

  submitSettlement(): void {
    if (!this.consignment) return;

    const itemsToSubmit = this.settlementItems.filter(
      (i) => (Number(i.quantitySold) || 0) > 0 || (Number(i.quantityReturned) || 0) > 0
    );

    if (itemsToSubmit.length === 0) {
      this.settlementError = 'يرجى إدخال كميات مباعة أو مرتجعة لصنف واحد على الأقل.';
      return;
    }

    for (const item of itemsToSubmit) {
      const sold = Number(item.quantitySold) || 0;
      const returned = Number(item.quantityReturned) || 0;
      if (sold < 0 || returned < 0) {
        this.settlementError = 'الكميات لا يمكن أن تكون سالبة.';
        return;
      }
      if (sold + returned > item.quantityOutstanding) {
        this.settlementError = `مجموع الكمية المباعة والمرتجعة للصنف (${item.itemDescription || item.rmsItemID}) يتجاوز الكمية المتبقية بالأمانة (${item.quantityOutstanding}).`;
        return;
      }
    }

    if (this.collectPaymentNow && Number(this.immediatePaymentAmount) > this.settlementDueTotal + 0.001) {
      this.settlementError = `مبلغ التحصيل أكبر من المستحق على البنزينة (${this.settlementDueTotal.toFixed(2)} ج.م).`;
      return;
    }

    const request: CreateConsignmentSettlementRequest = {
      notes: this.settlementNotes.trim() || undefined,
      createdBy: this.settlementCreatedBy.trim() || undefined,
      items: itemsToSubmit.map((i) => ({
        consignmentItemID: i.consignmentItemID,
        quantitySold: Number(i.quantitySold) || 0,
        quantityReturned: Number(i.quantityReturned) || 0
      }))
    };

    if (this.collectPaymentNow && this.immediatePaymentAmount > 0) {
      request.payment = {
        amount: Number(this.immediatePaymentAmount),
        paymentMethod: this.immediatePaymentMethod,
        notes: 'تحصيل فوري مع جلسة التسوية'
      };
    }

    this.savingSettlement = true;
    this.settlementError = '';

    this.consignmentService.addSettlement(this.consignment.id, request).subscribe({
      next: (updated) => {
        this.consignment = updated;
        this.savingSettlement = false;
        this.closeSettlementModal();
      },
      error: (err) => {
        this.settlementError = err.error?.message || 'حدث خطأ أثناء حفظ التسوية.';
        this.savingSettlement = false;
      }
    });
  }

  // ----------------------------------------------------
  // Payment Modal Logic
  // ----------------------------------------------------
  openPaymentModal(): void {
    if (!this.consignment) return;
    this.paymentRequest = {
      amount: this.consignment.remainingAmount > 0 ? this.consignment.remainingAmount : 0,
      paymentMethod: 'نقدي',
      notes: ''
    };
    this.paymentError = '';
    this.paymentMode = 'full';
    this.showPaymentModal = true;
  }

  closePaymentModal(): void {
    this.showPaymentModal = false;
  }

  submitPayment(): void {
    if (!this.consignment) return;
    if (!this.paymentRequest.amount || this.paymentRequest.amount <= 0) {
      this.paymentError = 'يرجى إدخال مبلغ صحيح أكبر من صفر.';
      return;
    }
    if (this.paymentRequest.amount > this.consignment.remainingAmount + 0.001) {
      this.paymentError = 'المبلغ أكبر من المتبقي للتحصيل على هذا الإذن.';
      return;
    }

    this.savingPayment = true;
    this.paymentError = '';

    this.consignmentService.addPayment(this.consignment.id, this.paymentRequest).subscribe({
      next: (updated) => {
        this.consignment = updated;
        this.savingPayment = false;
        this.closePaymentModal();
      },
      error: (err) => {
        this.paymentError = err.error?.message || 'حدث خطأ أثناء تسجيل الدفعة.';
        this.savingPayment = false;
      }
    });
  }

  // ----------------------------------------------------
  // Cancel Consignment
  // ----------------------------------------------------
  cancelConsignment(): void {
    if (!this.consignment) return;
    if (
      !confirm(
        `هل أنت متأكد من إلغاء إذن الأمانة ${this.consignment.consignmentNumber}؟ سيتم رد البضاعة إلى ${this.consignment.warehouseName || 'المخزن الرئيسي'}.`
      )
    ) {
      return;
    }

    this.cancelling = true;
    this.consignmentService.cancelConsignment(this.consignment.id).subscribe({
      next: (updated) => {
        this.consignment = updated;
        this.cancelling = false;
      },
      error: (err) => {
        alert(err.error?.message || 'حدث خطأ أثناء إلغاء إذن الأمانة.');
        this.cancelling = false;
      }
    });
  }

  printConsignment(): void {
    this.printer.print();
  }

  getAchievedProfit(): number {
    if (!this.consignment || !this.consignment.items) return 0;
    return this.consignment.items.reduce((sum, i) => sum + (i.profit || 0), 0);
  }

  get statusLabel(): string {
    const s = this.consignment?.status;
    if (this.isClosed(s)) return 'مسواة ومغلقة';
    if (this.isCancelled(s)) return 'ملغي';
    return this.consignment?.isOverdue ? 'قيد البيع — متأخر عن التسوية' : 'قيد البيع';
  }

  isOpen(status: any): boolean {
    return status === 'Open' || status === 1 || status === '1';
  }

  isClosed(status: any): boolean {
    return status === 'Closed' || status === 2 || status === '2';
  }

  isCancelled(status: any): boolean {
    return status === 'Cancelled' || status === 3 || status === '3';
  }

  /** Share (0-100) of the issued quantity already sold or returned. */
  get settledPercent(): number {
    const items = this.consignment?.items || [];
    const issued = items.reduce((s, i) => s + i.quantityIssued, 0);
    if (!issued) return 0;
    const done = items.reduce((s, i) => s + i.quantitySold + i.quantityReturned, 0);
    return Math.min(100, Math.round((done / issued) * 100));
  }

  get soldPercent(): number {
    const items = this.consignment?.items || [];
    const issued = items.reduce((s, i) => s + i.quantityIssued, 0);
    if (!issued) return 0;
    return Math.min(100, Math.round((items.reduce((s, i) => s + i.quantitySold, 0) / issued) * 100));
  }

  get daysToDue(): number {
    if (!this.consignment) return 0;
    const due = new Date(new Date(this.consignment.dueDate).toDateString());
    const today = new Date(new Date().toDateString());
    return Math.round((due.getTime() - today.getTime()) / 86400000);
  }

  canCancel(): boolean {
    const c = this.consignment;
    return !!c && this.isOpen(c.status) && !(c.settlements?.length) && !(c.payments?.length);
  }

  canSettle(): boolean {
    if (!this.consignment) return false;
    // Allow settling if status is Open or not Closed/Cancelled
    return !this.isClosed(this.consignment.status) && !this.isCancelled(this.consignment.status);
  }

  canPay(): boolean {
    if (!this.consignment) return false;
    return !this.isCancelled(this.consignment.status) && this.consignment.remainingAmount > 0;
  }
}

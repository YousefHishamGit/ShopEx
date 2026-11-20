import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ConsignmentService } from '../../../services/consignment.service';
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
  quantityOutstanding: number;
  unitPrice: number;
  unitCost: number;
  quantitySold: number;
  quantityReturned: number;
}

@Component({
  selector: 'app-consignment-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
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
    private consignmentService: ConsignmentService
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
        quantityOutstanding: i.quantityOutstanding,
        unitPrice: i.unitPrice,
        unitCost: i.unitCost,
        quantitySold: 0,
        quantityReturned: 0
      }));

    this.settlementNotes = '';
    this.settlementCreatedBy = '';
    this.collectPaymentNow = false;
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

  onSettlementSoldChange(): void {
    if (this.collectPaymentNow) {
      this.immediatePaymentAmount = this.totalSettlementSoldValue;
    }
  }

  toggleCollectPayment(): void {
    if (this.collectPaymentNow) {
      this.immediatePaymentAmount = this.totalSettlementSoldValue;
    }
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
        this.settlementError = `مجموع الكمية المباعة والمرتجعة للصنف (${item.rmsItemID}) يتجاوز الكمية المتبقية بالأمانة (${item.quantityOutstanding}).`;
        return;
      }
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
        `هل أنت متأكد من إلغاء إذن الأمانة ${this.consignment.consignmentNumber}؟ سيتم رد البضاعة لمخزن الـ HQ.`
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
    window.print();
  }

  getAchievedProfit(): number {
    if (!this.consignment || !this.consignment.items) return 0;
    return this.consignment.items.reduce((sum, i) => sum + (i.profit || 0), 0);
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

  canSettle(): boolean {
    if (!this.consignment) return false;
    // Allow settling if status is Open or not Closed/Cancelled
    return !this.isClosed(this.consignment.status) && !this.isCancelled(this.consignment.status);
  }

  canPay(): boolean {
    if (!this.consignment) return false;
    return !this.isCancelled(this.consignment.status) && (this.consignment.remainingAmount > 0 || !this.isClosed(this.consignment.status));
  }
}

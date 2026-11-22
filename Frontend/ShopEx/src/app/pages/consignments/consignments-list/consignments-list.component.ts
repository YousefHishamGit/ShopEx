import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ConsignmentService, PAYMENT_METHODS } from '../../../services/consignment.service';
import {
  ConsignmentSummary,
  Dealer,
  CreateDealerRequest,
  CreateConsignmentSettlementRequest,
  AddConsignmentPaymentRequest
} from '../../../models/models';

@Component({
  selector: 'app-consignments-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './consignments-list.component.html'
})
export class ConsignmentsListComponent implements OnInit {
  consignments: ConsignmentSummary[] = [];
  dealers: Dealer[] = [];
  loading = false;
  errorMessage = '';

  // Filters
  selectedStatus: string = 'Open';
  selectedDealerId: number | null = null;
  searchQuery: string = '';

  // Dealers modal
  showDealersModal = false;
  savingDealer = false;
  dealerError = '';
  newDealer: CreateDealerRequest = {
    name: '',
    phone: '',
    address: '',
    notes: ''
  };

  // Quick Payment modal
  showPaymentModal = false;
  paymentTargetConsignment: ConsignmentSummary | null = null;
  savingPayment = false;
  paymentError = '';
  paymentRequest: AddConsignmentPaymentRequest = {
    amount: 0,
    paymentMethod: 'نقدي',
    notes: ''
  };

  paymentMethods = PAYMENT_METHODS;
  paymentMode: 'full' | 'half' | 'custom' = 'full';

  constructor(private consignmentService: ConsignmentService) { }

  setPaymentMode(mode: 'full' | 'half' | 'custom'): void {
    this.paymentMode = mode;
    const remaining = this.paymentTargetConsignment?.remainingAmount || 0;
    if (mode === 'full') this.paymentRequest.amount = remaining;
    else if (mode === 'half') this.paymentRequest.amount = Math.round((remaining / 2) * 100) / 100;
  }

  ngOnInit(): void {
    this.loadDealers();
    this.loadConsignments();
  }

  loadDealers(): void {
    this.consignmentService.getDealers().subscribe({
      next: (data) => (this.dealers = data || []),
      error: (err) => console.error('Error loading dealers:', err)
    });
  }

  loadConsignments(): void {
    this.loading = true;
    this.errorMessage = '';

    // Only the dealer is filtered server-side: the status filters are applied locally so the
    // summary cards always reflect every consignment of the selected dealer (and no extra requests are needed).
    this.consignmentService
      .getConsignments(this.selectedDealerId || undefined)
      .subscribe({
        next: (data) => {
          this.consignments = data || [];
          this.loading = false;
        },
        error: (err) => {
          this.errorMessage = 'حدث خطأ أثناء تحميل أذونات البنزيمات.';
          this.loading = false;
        }
      });
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

  get filteredConsignments(): ConsignmentSummary[] {
    let list = this.consignments;

    if (this.selectedStatus === 'OVERDUE') {
      list = list.filter((c) => c.isOverdue);
    } else if (this.selectedStatus === 'Open') {
      list = list.filter((c) => this.isOpen(c.status));
    } else if (this.selectedStatus === 'Closed') {
      list = list.filter((c) => this.isClosed(c.status));
    } else if (this.selectedStatus === 'Cancelled') {
      list = list.filter((c) => this.isCancelled(c.status));
    }

    if (this.searchQuery && this.searchQuery.trim()) {
      const q = this.searchQuery.trim().toLowerCase();
      list = list.filter(
        (c) =>
          c.consignmentNumber.toLowerCase().includes(q) ||
          c.dealerName.toLowerCase().includes(q) ||
          c.dealerPhone.includes(q)
      );
    }

    return list;
  }

  // Statistics
  get countTotal(): number {
    return this.consignments.length;
  }

  get countOpen(): number {
    return this.consignments.filter((c) => this.isOpen(c.status)).length;
  }

  get countOverdue(): number {
    return this.consignments.filter((c) => c.isOverdue).length;
  }

  get countClosed(): number {
    return this.consignments.filter((c) => this.isClosed(c.status)).length;
  }

  get totalOutstandingValue(): number {
    return this.consignments
      .filter((c) => this.isOpen(c.status))
      .reduce((sum, c) => sum + (c.outstandingValue || 0), 0);
  }

  get totalRemainingUnpaid(): number {
    return this.consignments
      .filter((c) => this.isOpen(c.status))
      .reduce((sum, c) => sum + (c.remainingAmount || 0), 0);
  }

  onFilterStatus(status: string): void {
    this.selectedStatus = status;
  }

  trackById(_: number, item: { id: number }): number {
    return item.id;
  }

  onFilterDealer(): void {
    this.loadConsignments();
  }

  // Dealers Management Modal
  openDealersModal(): void {
    this.newDealer = { name: '', phone: '', address: '', notes: '' };
    this.dealerError = '';
    this.showDealersModal = true;
    this.loadDealers();
  }

  closeDealersModal(): void {
    this.showDealersModal = false;
  }

  saveNewDealer(): void {
    if (!this.newDealer.name || !this.newDealer.name.trim()) {
      this.dealerError = 'يرجى إدخال اسم البنزينة / المحل.';
      return;
    }
    if (!this.newDealer.phone || !this.newDealer.phone.trim()) {
      this.dealerError = 'يرجى إدخال رقم الهاتف.';
      return;
    }

    this.savingDealer = true;
    this.dealerError = '';
    this.consignmentService.createDealer(this.newDealer).subscribe({
      next: (created) => {
        this.dealers.push(created);
        this.savingDealer = false;
        this.newDealer = { name: '', phone: '', address: '', notes: '' };
      },
      error: (err) => {
        this.dealerError = err.error?.message || 'حدث خطأ أثناء حفظ البنزينة.';
        this.savingDealer = false;
      }
    });
  }

  // Quick Payment Modal
  openPaymentModal(item: ConsignmentSummary, event: MouseEvent): void {
    event.stopPropagation();
    this.paymentTargetConsignment = item;
    this.paymentRequest = {
      amount: item.remainingAmount > 0 ? item.remainingAmount : 0,
      paymentMethod: 'نقدي',
      notes: ''
    };
    this.paymentError = '';
    this.paymentMode = 'full';
    this.showPaymentModal = true;
  }

  closePaymentModal(): void {
    this.showPaymentModal = false;
    this.paymentTargetConsignment = null;
  }

  submitPayment(): void {
    if (!this.paymentTargetConsignment) return;
    if (!this.paymentRequest.amount || this.paymentRequest.amount <= 0) {
      this.paymentError = 'يرجى إدخال مبلغ صحيح أكبر من صفر.';
      return;
    }
    if (this.paymentRequest.amount > this.paymentTargetConsignment.remainingAmount + 0.001) {
      this.paymentError = 'المبلغ أكبر من المتبقي للتحصيل على هذا الإذن.';
      return;
    }

    this.savingPayment = true;
    this.paymentError = '';

    this.consignmentService
      .addPayment(this.paymentTargetConsignment.id, this.paymentRequest)
      .subscribe({
        next: () => {
          this.savingPayment = false;
          this.closePaymentModal();
          this.loadConsignments();
          this.loadDealers();
        },
        error: (err) => {
          this.paymentError = err.error?.message || 'حدث خطأ أثناء تسجيل الدفعة.';
          this.savingPayment = false;
        }
      });
  }
}

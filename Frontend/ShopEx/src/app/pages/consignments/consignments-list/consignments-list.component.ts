import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
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
export class ConsignmentsListComponent implements OnInit, OnDestroy {
  /** The current page only: filtering, search and paging all happen on the server. */
  consignments: ConsignmentSummary[] = [];

  // Paging
  page = 1;
  readonly pageSize = 25;
  totalPages = 1;
  totalCount = 0;

  // Stats for the selected dealer (independent of the status tab and the search box)
  countTotal = 0;
  countOpen = 0;
  countOverdue = 0;
  countClosed = 0;
  totalOutstandingValue = 0;
  totalRemainingUnpaid = 0;

  private search$ = new Subject<string>();
  private searchSub?: Subscription;
  private loadSub?: Subscription;
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
    this.searchSub = this.search$
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe(() => {
        this.page = 1;
        this.loadConsignments();
      });
    this.loadDealers();
    this.loadConsignments();
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
    this.loadSub?.unsubscribe();
  }

  onSearchChange(): void {
    this.search$.next(this.searchQuery.trim());
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages || p === this.page || this.loading) return;
    this.page = p;
    this.loadConsignments();
  }

  get rangeStart(): number {
    return this.totalCount === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  loadDealers(): void {
    this.consignmentService.getDealers().subscribe({
      next: (data) => (this.dealers = data || []),
      error: (err) => console.error('Error loading dealers:', err)
    });
  }

  loadConsignments(): void {
    // Cancel any in-flight request so an older, slower response never overwrites a newer one
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.errorMessage = '';

    this.loadSub = this.consignmentService
      .getConsignmentsPaged(this.selectedDealerId || undefined, this.selectedStatus, this.searchQuery.trim(), this.page, this.pageSize)
      .subscribe({
        next: (res) => {
          this.consignments = res.items || [];
          this.totalCount = res.totalCount;
          this.totalPages = res.totalPages || 1;
          this.countTotal = res.countTotal;
          this.countOpen = res.countOpen;
          this.countOverdue = res.countOverdue;
          this.countClosed = res.countClosed;
          this.totalOutstandingValue = res.totalOutstandingValue;
          this.totalRemainingUnpaid = res.totalRemainingUnpaid;
          this.loading = false;
        },
        error: () => {
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

  /** Kept for the template: the server already applied the status tab and the search. */
  get filteredConsignments(): ConsignmentSummary[] {
    return this.consignments;
  }

  onFilterStatus(status: string): void {
    if (this.selectedStatus === status) return;
    this.selectedStatus = status;
    this.page = 1;
    this.loadConsignments();
  }

  trackById(_: number, item: { id: number }): number {
    return item.id;
  }

  onFilterDealer(): void {
    this.page = 1;
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

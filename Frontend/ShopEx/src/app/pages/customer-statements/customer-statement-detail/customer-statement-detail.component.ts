import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CustomerStatementService } from '../../../services/customer-statement.service';
import { PrintService } from '../../../shared/print/print.service';
import { PrintSheetComponent } from '../../../shared/print/print-sheet.component';
import { CustomerInvoicePrintComponent } from '../../../shared/print/customer-invoice-print.component';
import {
  CustomerStatement,
  CustomerOpenInvoice,
  CustomerInvoiceDetail
} from '../../../models/models';

@Component({
  selector: 'app-customer-statement-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PrintSheetComponent, CustomerInvoicePrintComponent],
  templateUrl: './customer-statement-detail.component.html'
})
export class CustomerStatementDetailComponent implements OnInit {
  customerId!: number;
  statement: CustomerStatement | null = null;
  filteredInvoices: CustomerOpenInvoice[] = [];
  loading = true;
  errorMessage = '';

  // Filter
  minDaysOld: number | null = 15;
  searchQuery = '';

  // Invoice Details Modal
  showInvoiceModal = false;
  selectedInvoiceDetail: CustomerInvoiceDetail | null = null;
  loadingInvoiceDetail = false;
  invoiceModalError = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private statementService: CustomerStatementService,
    private printer: PrintService
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.customerId = Number(idParam);
      this.loadStatement();
    } else {
      this.errorMessage = 'معرف العميل غير صحيح';
      this.loading = false;
    }
  }

  loadStatement(): void {
    this.loading = true;
    this.errorMessage = '';
    this.statementService
      .getCustomerStatement(this.customerId, this.minDaysOld || undefined)
      .subscribe({
        next: (res) => {
          this.statement = res;
          this.applyLocalInvoiceFilter();
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading statement:', err);
          this.errorMessage = err.error?.message || 'تعذر تحميل كشف حساب العميل من قاعدة بيانات RMS.';
          this.loading = false;
        }
      });
  }

  onFilterChange(): void {
    this.loadStatement();
  }

  setQuickDays(days: number | null): void {
    this.minDaysOld = days;
    this.loadStatement();
  }

  applyLocalInvoiceFilter(): void {
    if (!this.statement) {
      this.filteredInvoices = [];
      return;
    }

    let list = this.statement.openInvoices || [];
    if (this.searchQuery && this.searchQuery.trim() !== '') {
      const q = this.searchQuery.trim().toLowerCase();
      list = list.filter(
        (inv) =>
          inv.invoiceID.toString().includes(q) ||
          (inv.storeName && inv.storeName.toLowerCase().includes(q))
      );
    }
    this.filteredInvoices = list;
  }

  onSearchQueryChange(): void {
    this.applyLocalInvoiceFilter();
  }

  getDaysOldClass(days: number): string {
    if (days >= 30) return 'bg-error/10 text-error border border-error/20';
    if (days >= 15) return 'bg-amber-100 text-amber-800 border border-amber-300';
    return 'bg-blue-50 text-blue-700 border border-blue-200';
  }

  // ----------------------------------------------------
  // Invoice Details Modal
  // ----------------------------------------------------
  openInvoiceModal(orderId: number): void {
    this.showInvoiceModal = true;
    this.loadingInvoiceDetail = true;
    this.selectedInvoiceDetail = null;
    this.invoiceModalError = '';

    this.statementService.getInvoiceDetail(orderId).subscribe({
      next: (detail) => {
        this.selectedInvoiceDetail = detail;
        this.loadingInvoiceDetail = false;
      },
      error: (err) => {
        this.invoiceModalError = err.error?.message || 'تعذر تحميل تفاصيل الفاتورة.';
        this.loadingInvoiceDetail = false;
      }
    });
  }

  closeInvoiceModal(): void {
    this.showInvoiceModal = false;
    this.selectedInvoiceDetail = null;
  }

  /** Which official document is currently being printed */
  printMode: 'statement' | 'invoice' = 'statement';

  get filteredTotal(): number {
    return this.filteredInvoices.reduce((s, i) => s + (i.totalAmount || 0), 0);
  }

  get filteredDeposit(): number {
    return this.filteredInvoices.reduce((s, i) => s + (i.deposit || 0), 0);
  }

  get filteredRemaining(): number {
    return this.filteredInvoices.reduce((s, i) => s + (i.remainingAmount || 0), 0);
  }

  printInvoiceModal(): void {
    this.printMode = 'invoice';
    this.printer.print();
  }

  printStatement(): void {
    this.printMode = 'statement';
    this.printer.print();
  }
}

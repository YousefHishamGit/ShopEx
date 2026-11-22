import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { SupplierInvoice } from '../../models/models';

@Component({
  selector: 'app-invoices-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './invoices-list.component.html'
})
export class InvoicesListComponent implements OnInit {
  invoices: SupplierInvoice[] = [];
  searchTerm: string = '';
  statusFilter: string = 'ALL';
  loading: boolean = true;
  errorMessage: string = '';

  // Server-side pagination: only the visible page is ever loaded
  currentPage: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  totalPages: number = 1;

  // Totals over the whole filtered set (computed by the server)
  totalInvoicedAmount: number = 0;
  totalPaidAmount: number = 0;
  totalRemainingAmount: number = 0;

  private searchTimeout: any;
  private requestSeq = 0;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.fetchInvoices(1);
  }

  fetchInvoices(page: number = this.currentPage): void {
    this.loading = true;
    this.errorMessage = '';
    const seq = ++this.requestSeq;

    this.api.getInvoicesPaged(page, this.pageSize, this.searchTerm, this.statusFilter).subscribe({
      next: (res) => {
        if (seq !== this.requestSeq) return; // a newer request superseded this one
        this.invoices = res.items || [];
        this.totalCount = res.totalCount;
        this.currentPage = res.pageNumber;
        this.pageSize = res.pageSize;
        this.totalPages = res.totalPages || 1;
        this.totalInvoicedAmount = res.totalAmount;
        this.totalPaidAmount = res.totalPaid;
        this.totalRemainingAmount = res.totalRemaining;
        this.loading = false;
      },
      error: (err) => {
        if (seq !== this.requestSeq) return;
        console.error('Error fetching invoices', err);
        this.errorMessage = 'تعذر تحميل الفواتير. تأكد من تشغيل السيرفر ثم أعد المحاولة.';
        this.loading = false;
      }
    });
  }

  onSearchChange(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => this.fetchInvoices(1), 350);
  }

  onStatusChange(): void {
    this.fetchInvoices(1);
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = 'ALL';
    this.fetchInvoices(1);
  }

  get hasFilters(): boolean {
    return !!this.searchTerm.trim() || this.statusFilter !== 'ALL';
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.fetchInvoices(page);
    }
  }

  onPageSizeChange(): void {
    this.fetchInvoices(1);
  }

  get pageNumbers(): number[] {
    const pages: number[] = [];
    let start = Math.max(1, this.currentPage - 2);
    const end = Math.min(this.totalPages, start + 4);
    start = Math.max(1, end - 4);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  isOverdue(inv: SupplierInvoice): boolean {
    return !!inv.dueDate && inv.remainingAmount > 0 && new Date(inv.dueDate) < new Date(new Date().toDateString());
  }
}

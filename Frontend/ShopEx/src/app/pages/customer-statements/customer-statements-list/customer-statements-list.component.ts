import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CustomerStatementService } from '../../../services/customer-statement.service';
import { PrintService } from '../../../shared/print/print.service';
import { CustomerInvoicePrintComponent } from '../../../shared/print/customer-invoice-print.component';
import { ApiService } from '../../../services/api.service';
import {
  CustomerOpenInvoice,
  CustomerSummary,
  CustomerInvoiceDetail,
  StoreOption
} from '../../../models/models';

@Component({
  selector: 'app-customer-statements-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, CustomerInvoicePrintComponent],
  templateUrl: './customer-statements-list.component.html'
})
export class CustomerStatementsListComponent implements OnInit {
  activeTab: 'invoices' | 'customers' = 'invoices';

  // Stores from DB
  stores: StoreOption[] = [];
  selectedStoreId: number | null = null;

  // Filters
  searchQuery = '';
  minDaysOld: number | null = 15; // Default 15 days as requested by user
  searchDebounceTimer: any;

  // Invoices Tab Data
  invoices: CustomerOpenInvoice[] = [];
  invoicesTotalCount = 0;
  invoicesPageNumber = 1;
  invoicesPageSize = 20;
  invoicesTotalPages = 1;
  loadingInvoices = false;

  // Customers Tab Data
  customers: CustomerSummary[] = [];
  customersTotalCount = 0;
  customersPageNumber = 1;
  customersPageSize = 20;
  customersTotalPages = 1;
  loadingCustomers = false;

  // Invoice Details Modal
  showInvoiceModal = false;
  selectedInvoiceDetail: CustomerInvoiceDetail | null = null;
  loadingInvoiceDetail = false;
  invoiceModalError = '';

  constructor(
    private statementService: CustomerStatementService,
    private api: ApiService,
    private router: Router,
    private printer: PrintService
  ) {}

  ngOnInit(): void {
    this.loadStores();
    this.loadInvoices();
  }

  loadStores(): void {
    this.api.getStores().subscribe({
      next: (stores) => {
        this.stores = stores || [];
      },
      error: (err) => console.error('Failed to load stores for customer statements filter', err)
    });
  }

  onStoreChange(): void {
    this.invoicesPageNumber = 1;
    this.loadInvoices();
  }

  setTab(tab: 'invoices' | 'customers'): void {
    this.activeTab = tab;
    if (tab === 'invoices') {
      this.loadInvoices();
    } else {
      this.loadCustomers();
    }
  }

  // ----------------------------------------------------
  // Load Invoices
  // ----------------------------------------------------
  loadInvoices(): void {
    this.loadingInvoices = true;
    this.statementService
      .getOpenInvoices(
        undefined,
        this.selectedStoreId || undefined,
        this.minDaysOld || undefined,
        this.searchQuery,
        this.invoicesPageNumber,
        this.invoicesPageSize
      )
      .subscribe({
        next: (res) => {
          this.invoices = res.items || [];
          this.invoicesTotalCount = res.totalCount;
          this.invoicesPageNumber = res.pageNumber;
          this.invoicesPageSize = res.pageSize;
          this.invoicesTotalPages = res.totalPages;
          this.loadingInvoices = false;
        },
        error: (err) => {
          console.error('Error loading invoices:', err);
          this.loadingInvoices = false;
        }
      });
  }

  // ----------------------------------------------------
  // Load Customers
  // ----------------------------------------------------
  loadCustomers(): void {
    this.loadingCustomers = true;
    this.statementService
      .getCustomers(
        this.searchQuery,
        this.minDaysOld || undefined,
        this.customersPageNumber,
        this.customersPageSize
      )
      .subscribe({
        next: (res) => {
          this.customers = res.items || [];
          this.customersTotalCount = res.totalCount;
          this.customersPageNumber = res.pageNumber;
          this.customersPageSize = res.pageSize;
          this.customersTotalPages = res.totalPages;
          this.loadingCustomers = false;
        },
        error: (err) => {
          console.error('Error loading customers:', err);
          this.loadingCustomers = false;
        }
      });
  }

  // ----------------------------------------------------
  // Filter Triggers
  // ----------------------------------------------------
  onSearchChange(): void {
    clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => {
      this.resetPageAndReload();
    }, 400);
  }

  onDaysFilterChange(): void {
    this.resetPageAndReload();
  }

  setQuickDays(days: number | null): void {
    this.minDaysOld = days;
    this.resetPageAndReload();
  }

  resetPageAndReload(): void {
    if (this.activeTab === 'invoices') {
      this.invoicesPageNumber = 1;
      this.loadInvoices();
    } else {
      this.customersPageNumber = 1;
      this.loadCustomers();
    }
  }

  // ----------------------------------------------------
  // Pagination Methods
  // ----------------------------------------------------
  goToInvoicesPage(page: number): void {
    if (page >= 1 && page <= this.invoicesTotalPages) {
      this.invoicesPageNumber = page;
      this.loadInvoices();
    }
  }

  goToCustomersPage(page: number): void {
    if (page >= 1 && page <= this.customersTotalPages) {
      this.customersPageNumber = page;
      this.loadCustomers();
    }
  }

  onPageSizeChange(): void {
    this.resetPageAndReload();
  }

  // ----------------------------------------------------
  // Invoice Details Modal
  // ----------------------------------------------------
  openInvoiceModal(orderId: number, event?: MouseEvent): void {
    if (event) event.stopPropagation();
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

  printInvoiceModal(): void {
    this.printer.print();
  }
}

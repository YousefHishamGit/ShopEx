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
  filteredInvoices: SupplierInvoice[] = [];
  searchTerm: string = '';
  statusFilter: string = 'ALL';
  loading: boolean = true;
  errorMessage: string = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.fetchInvoices();
  }

  fetchInvoices(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.getInvoices().subscribe({
      next: (data) => {
        this.invoices = data || [];
        this.applyFilter();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching invoices', err);
        this.errorMessage = 'Failed to load invoices. Ensure backend is running.';
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    let result = [...this.invoices];
    const term = this.searchTerm.toLowerCase().trim();

    if (term) {
      result = result.filter(inv =>
        (inv.invoiceNumber && inv.invoiceNumber.toLowerCase().includes(term)) ||
        (inv.rmsSupplierName && inv.rmsSupplierName.toLowerCase().includes(term)) ||
        (inv.rmsSupplierID && inv.rmsSupplierID.toString().includes(term))
      );
    }

    if (this.statusFilter !== 'ALL') {
      result = result.filter(inv => inv.statusName?.toLowerCase() === this.statusFilter.toLowerCase());
    }

    this.filteredInvoices = result;
  }

  get totalInvoicesCount(): number {
    return this.invoices.length;
  }

  get totalInvoicedAmount(): number {
    return this.invoices.reduce((sum, inv) => sum + (inv.totalAmount || 0), 0);
  }

  get totalPaidAmount(): number {
    return this.invoices.reduce((sum, inv) => sum + (inv.paidAmount || 0), 0);
  }

  get totalRemainingAmount(): number {
    return this.invoices.reduce((sum, inv) => sum + (inv.remainingAmount || 0), 0);
  }
}


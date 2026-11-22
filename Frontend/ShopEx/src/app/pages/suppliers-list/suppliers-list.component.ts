import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Supplier } from '../../models/models';

@Component({
  selector: 'app-suppliers-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './suppliers-list.component.html'
})
export class SuppliersListComponent implements OnInit {
  suppliers: Supplier[] = [];
  searchTerm: string = '';
  loading: boolean = true;
  errorMessage: string = '';

  // Server-Side Pagination
  currentPage: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  totalPages: number = 1;

  private searchTimeout: any;

  constructor(private api: ApiService, private router: Router) { }

  ngOnInit(): void {
    this.fetchSuppliers(1);
  }

  fetchSuppliers(page: number = 1): void {
    this.loading = true;
    this.errorMessage = '';
    this.currentPage = page;

    // Every page is a request to the server for performance
    this.api.getSuppliers(this.currentPage, this.pageSize, this.searchTerm).subscribe({
      next: (res) => {
        this.suppliers = res.items || [];


        this.totalCount = res.totalCount;
        this.currentPage = res.pageNumber;
        this.pageSize = res.pageSize;
        this.totalPages = res.totalPages || 1;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching suppliers page', err);
        this.errorMessage = 'فشل في تحميل الموردين من RMS. يرجى التأكد من تشغيل السيرفر.';
        this.loading = false;
      }
    });
  }

  onSearchChange(): void {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    // Debounce search input to avoid spamming requests
    this.searchTimeout = setTimeout(() => {
      this.fetchSuppliers(1);
    }, 350);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.fetchSuppliers(1);
  }

  changePage(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages && newPage !== this.currentPage) {
      this.fetchSuppliers(newPage);
    }
  }

  onPageSizeChange(): void {
    this.fetchSuppliers(1);
  }

  viewSupplierStatement(supplierId: number): void {
    this.router.navigate(['/suppliers', supplierId]);
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;
    let start = Math.max(1, this.currentPage - 2);
    let end = Math.min(this.totalPages, start + maxVisible - 1);

    if (end - start < maxVisible - 1) {
      start = Math.max(1, end - maxVisible + 1);
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }
}

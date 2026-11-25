import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { MaintenanceService } from '../../../services/maintenance.service';
import { ExpenseService } from '../../../services/expense.service';
import { MaintenanceTicketSummary, MaintenanceStatus } from '../../../models/models';

@Component({
  selector: 'app-maintenance-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './maintenance-list.component.html'
})
export class MaintenanceListComponent implements OnInit, OnDestroy {
  filteredTickets: MaintenanceTicketSummary[] = [];
  searchTerm: string = '';
  selectedStatus: string = '1';
  loading: boolean = true;
  errorMessage: string = '';

  // Pagination (server side)
  page = 1;
  readonly pageSize = 25;
  totalCount = 0;
  totalPages = 1;

  // إحصائيات كل التذاكر (من السيرفر)
  countTotal = 0;
  countReceived = 0;
  countCompleted = 0;
  countDelivered = 0;

  // فلتر الفرع (الأدمن) + إجماليات التكلفة والربح للنتائج الحالية
  stores: { id: number; name: string }[] = [];
  selectedStoreId: number | null = null;
  totalPartCost = 0;
  totalProfit = 0;

  // Enums for template usage
  MaintenanceStatus = MaintenanceStatus;

  private loadSub?: Subscription;
  private search$ = new Subject<void>();
  private searchSub?: Subscription;

  constructor(private maintenanceService: MaintenanceService, private expenseService: ExpenseService) { }

  ngOnInit(): void {
    this.searchSub = this.search$.pipe(debounceTime(350)).subscribe(() => {
      this.page = 1;
      this.fetchTickets();
    });
    this.expenseService.getStores().subscribe({
      next: (list) => (this.stores = list || []),
      error: () => (this.stores = [])
    });
    this.fetchTickets();
  }

  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
    this.searchSub?.unsubscribe();
  }

  fetchTickets(): void {
    // إلغاء الطلب السابق حتى لا يكتب رد قديم فوق رد أحدث
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.errorMessage = '';

    const status = this.selectedStatus === 'ALL' ? undefined : Number(this.selectedStatus) as MaintenanceStatus;

    this.loadSub = this.maintenanceService.getTicketsPaged(this.page, this.pageSize, status, this.searchTerm, this.selectedStoreId).subscribe({
      next: (res) => {
        this.filteredTickets = res.items || [];
        this.page = res.page;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.countTotal = res.countTotal;
        this.countReceived = res.countReceived;
        this.countCompleted = res.countCompleted;
        this.countDelivered = res.countDelivered;
        this.totalPartCost = res.totalPartCost ?? 0;
        this.totalProfit = res.totalProfit ?? 0;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching maintenance tickets', err);
        this.errorMessage = 'تعذر تحميل تذاكر الصيانة. يرجى التأكد من تشغيل السيرفر.';
        this.loading = false;
      }
    });
  }

  onSearchInput(): void {
    this.search$.next();
  }

  onStoreChange(): void {
    this.page = 1;
    this.fetchTickets();
  }

  onFilterStatus(status: string): void {
    this.selectedStatus = status;
    this.page = 1;
    this.fetchTickets();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages || p === this.page) return;
    this.page = p;
    this.fetchTickets();
  }

  get rangeStart(): number {
    return this.totalCount === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  getStatusBadgeClass(status: MaintenanceStatus): string {
    switch (status) {
      case MaintenanceStatus.Received:
        return 'bg-blue-50 text-blue-700 border-blue-200';
      case MaintenanceStatus.UnderRepair:
        return 'bg-amber-50 text-amber-700 border-amber-200';
      case MaintenanceStatus.Completed:
        return 'bg-purple-50 text-purple-700 border-purple-200';
      case MaintenanceStatus.Delivered:
        return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case MaintenanceStatus.Cancelled:
        return 'bg-rose-50 text-rose-700 border-rose-200';
      default:
        return 'bg-gray-50 text-gray-700 border-gray-200';
    }
  }

  getStatusArabicName(status: MaintenanceStatus): string {
    switch (status) {
      case MaintenanceStatus.Received:
        return '1. تم الاستلام';
      case MaintenanceStatus.UnderRepair:
        return '2. قيد الإصلاح';
      case MaintenanceStatus.Completed:
        return '3. تم الإصلاح (جاهز)';
      case MaintenanceStatus.Delivered:
        return '4. تم التسليم والدفع';
      case MaintenanceStatus.Cancelled:
        return 'ملغي';
      default:
        return 'غير محدد';
    }
  }
}

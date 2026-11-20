import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MaintenanceService } from '../../../services/maintenance.service';
import { MaintenanceTicketSummary, MaintenanceStatus } from '../../../models/models';

@Component({
  selector: 'app-maintenance-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './maintenance-list.component.html'
})
export class MaintenanceListComponent implements OnInit {
  tickets: MaintenanceTicketSummary[] = [];
  filteredTickets: MaintenanceTicketSummary[] = [];
  searchTerm: string = '';
  selectedStatus: string = 'ALL';
  loading: boolean = true;
  errorMessage: string = '';

  // Enums for template usage
  MaintenanceStatus = MaintenanceStatus;

  constructor(private maintenanceService: MaintenanceService) {}

  ngOnInit(): void {
    this.fetchTickets();
  }

  fetchTickets(): void {
    this.loading = true;
    this.errorMessage = '';
    this.maintenanceService.getTickets().subscribe({
      next: (data) => {
        this.tickets = data || [];
        this.applyFilter();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching maintenance tickets', err);
        this.errorMessage = 'تعذر تحميل تذاكر الصيانة. يرجى التأكد من تشغيل السيرفر.';
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    let result = [...this.tickets];
    const term = this.searchTerm.toLowerCase().trim();

    if (term) {
      result = result.filter(t =>
        (t.ticketNumber && t.ticketNumber.toLowerCase().includes(term)) ||
        (t.customerName && t.customerName.toLowerCase().includes(term)) ||
        (t.customerPhone && t.customerPhone.includes(term)) ||
        (t.deviceName && t.deviceName.toLowerCase().includes(term))
      );
    }

    if (this.selectedStatus !== 'ALL') {
      const statusNum = Number(this.selectedStatus);
      result = result.filter(t => t.status === statusNum);
    }

    this.filteredTickets = result;
  }

  onFilterStatus(status: string): void {
    this.selectedStatus = status;
    this.applyFilter();
  }

  // إحصائيات سريعة
  get countTotal(): number {
    return this.tickets.length;
  }

  get countReceived(): number {
    return this.tickets.filter(t => t.status === MaintenanceStatus.Received || t.status === MaintenanceStatus.UnderRepair).length;
  }

  get countCompleted(): number {
    return this.tickets.filter(t => t.status === MaintenanceStatus.Completed).length;
  }

  get countDelivered(): number {
    return this.tickets.filter(t => t.status === MaintenanceStatus.Delivered).length;
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

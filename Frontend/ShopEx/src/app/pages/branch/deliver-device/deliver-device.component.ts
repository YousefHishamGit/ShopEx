import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaintenanceService } from '../../../services/maintenance.service';
import { ToastService } from '../../../services/toast.service';
import { MaintenancePaymentMethod, MaintenanceStatus, MaintenanceTicketSummary } from '../../../models/models';

@Component({
  selector: 'app-deliver-device',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './deliver-device.component.html'
})
export class DeliverDeviceComponent implements OnInit {
  readonly MaintenancePaymentMethod = MaintenancePaymentMethod;

  tickets: MaintenanceTicketSummary[] = [];
  filtered: MaintenanceTicketSummary[] = [];
  searchQuery = '';

  loading = false;
  errorMessage = '';

  // Delivery modal
  target: MaintenanceTicketSummary | null = null;
  paymentAmount: number | null = null;
  paymentMethod: MaintenancePaymentMethod = MaintenancePaymentMethod.Cash;
  deliveryNotes = '';
  submitting = false;
  modalError = '';

  constructor(
    private maintenanceService: MaintenanceService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  /** Only devices that are repaired and ready; the server also limits them to this branch. */
  load(): void {
    this.loading = true;
    this.errorMessage = '';
    this.maintenanceService.getTickets(MaintenanceStatus.Completed).subscribe({
      next: (data) => {
        this.tickets = data || [];
        this.applyFilter();
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'تعذر تحميل الأجهزة الجاهزة.';
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    const q = this.searchQuery.trim().toLowerCase();
    this.filtered = !q
      ? this.tickets
      : this.tickets.filter(
          (t) =>
            t.ticketNumber.toLowerCase().includes(q) ||
            t.customerName.toLowerCase().includes(q) ||
            t.customerPhone.includes(q) ||
            t.deviceName.toLowerCase().includes(q)
        );
  }

  trackTicket = (_: number, t: MaintenanceTicketSummary) => t.id;

  openModal(ticket: MaintenanceTicketSummary): void {
    this.target = ticket;
    this.paymentAmount = ticket.remainingAmount > 0 ? ticket.remainingAmount : null;
    this.paymentMethod = MaintenancePaymentMethod.Cash;
    this.deliveryNotes = '';
    this.modalError = '';
  }

  closeModal(): void {
    if (this.submitting) return;
    this.target = null;
  }

  submit(): void {
    if (!this.target || this.submitting) return;

    const remaining = this.target.remainingAmount;
    const amount = this.paymentAmount === null || this.paymentAmount === undefined ? 0 : Number(this.paymentAmount);

    if (amount < 0) {
      this.modalError = 'المبلغ لا يمكن أن يكون سالباً.';
      return;
    }
    if (remaining > 0 && amount < remaining) {
      this.modalError = `يجب تحصيل كامل المبلغ المتبقي (${remaining}) قبل التسليم.`;
      return;
    }

    const ticket = this.target;
    this.submitting = true;
    this.modalError = '';

    this.maintenanceService
      .deliverTicket(ticket.id, {
        paymentAmount: remaining > 0 ? amount : undefined,
        paymentMethod: this.paymentMethod,
        deliveryNotes: this.deliveryNotes
      })
      .subscribe({
        next: () => {
          this.submitting = false;
          this.target = null;
          this.toast.success(`تم تسليم الجهاز (${ticket.deviceName}) للعميل ${ticket.customerName}.`);
          this.load();
        },
        error: (err) => {
          this.submitting = false;
          this.modalError = err.error?.message || 'تعذر تسليم الجهاز.';
        }
      });
  }
}

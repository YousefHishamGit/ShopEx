import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MaintenanceService } from '../../../services/maintenance.service';
import { ToastService } from '../../../services/toast.service';
import { CreateMaintenanceTicketRequest, MaintenancePaymentMethod } from '../../../models/models';

@Component({
  selector: 'app-maintenance-create',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './maintenance-create.component.html'
})
export class MaintenanceCreateComponent {
  // Form model
  request: CreateMaintenanceTicketRequest = {
    receivedBy: '',
    customerName: '',
    customerPhone: '',
    deviceName: '',
    brand: '',
    model: '',
    color: '',
    serialNumber: '',
    issueDescription: '',
    accessories: '',
    notes: '',
    expectedCost: 0,
    deposit: undefined,
    depositPaymentMethod: MaintenancePaymentMethod.Cash
  };

  submitting: boolean = false;
  errorMessage: string = '';
  MaintenancePaymentMethod = MaintenancePaymentMethod;

  constructor(
    private maintenanceService: MaintenanceService,
    private toast: ToastService,
    private router: Router
  ) {}

  onSubmit(): void {
    this.errorMessage = '';

    if (!this.request.customerName?.trim()) {
      this.errorMessage = 'يرجى كتابة اسم العميل.';
      this.toast.error(this.errorMessage);
      return;
    }

    if (!this.request.customerPhone?.trim()) {
      this.errorMessage = 'يرجى كتابة رقم هاتف العميل.';
      this.toast.error(this.errorMessage);
      return;
    }

    if (!this.request.deviceName?.trim()) {
      this.errorMessage = 'يرجى كتابة اسم أو نوع الجهاز المستلم.';
      this.toast.error(this.errorMessage);
      return;
    }

    this.submitting = true;
    this.maintenanceService.createTicket(this.request).subscribe({
      next: (ticket) => {
        this.submitting = false;
        this.toast.success(`تم استلام وتسجيل جهاز (${ticket.deviceName}) بنجاح برقم تذكرة #${ticket.id}`);
        this.router.navigate(['/maintenance', ticket.id]);
      },
      error: (err) => {
        console.error('Error creating maintenance ticket', err);
        this.errorMessage = err.error?.message || 'حدث خطأ أثناء حفظ تذكرة الصيانة.';
        this.toast.error(this.errorMessage);
        this.submitting = false;
      }
    });
  }
}

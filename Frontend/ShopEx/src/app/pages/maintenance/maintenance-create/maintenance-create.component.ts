import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MaintenanceService } from '../../../services/maintenance.service';
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
    private router: Router
  ) {}

  onSubmit(): void {
    this.errorMessage = '';

    if (!this.request.customerName?.trim()) {
      this.errorMessage = 'يرجى كتابة اسم العميل.';
      return;
    }

    if (!this.request.customerPhone?.trim()) {
      this.errorMessage = 'يرجى كتابة رقم هاتف العميل.';
      return;
    }

    if (!this.request.deviceName?.trim()) {
      this.errorMessage = 'يرجى كتابة اسم أو نوع الجهاز المستلم.';
      return;
    }

    this.submitting = true;
    this.maintenanceService.createTicket(this.request).subscribe({
      next: (ticket) => {
        this.submitting = false;
        // Direct redirect to ticket detail page so they can view, print receipt, or start repair
        this.router.navigate(['/maintenance', ticket.id]);
      },
      error: (err) => {
        console.error('Error creating maintenance ticket', err);
        this.errorMessage = err.error?.message || 'حدث خطأ أثناء حفظ تذكرة الصيانة.';
        this.submitting = false;
      }
    });
  }
}

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaintenanceService } from '../../../services/maintenance.service';
import { ToastService } from '../../../services/toast.service';
import { CreateMaintenanceTicketRequest, MaintenancePaymentMethod, MaintenanceTicket } from '../../../models/models';

@Component({
  selector: 'app-receive-device',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './receive-device.component.html'
})
export class ReceiveDeviceComponent {
  readonly MaintenancePaymentMethod = MaintenancePaymentMethod;

  request: CreateMaintenanceTicketRequest = this.emptyRequest();
  submitting = false;
  errorMessage = '';

  /** The last ticket created, shown as a confirmation (the receipt number the customer needs). */
  lastTicket: MaintenanceTicket | null = null;

  constructor(
    private maintenanceService: MaintenanceService,
    private toast: ToastService
  ) {}

  private emptyRequest(): CreateMaintenanceTicketRequest {
    return {
      customerName: '',
      customerPhone: '',
      deviceName: '',
      brand: '',
      model: '',
      serialNumber: '',
      issueDescription: '',
      accessories: '',
      expectedCost: 0,
      deposit: undefined,
      depositPaymentMethod: MaintenancePaymentMethod.Cash
    };
  }

  onSubmit(): void {
    if (this.submitting) return;
    this.errorMessage = '';

    if (!this.request.customerName.trim()) return this.fail('يرجى كتابة اسم العميل.');
    if (!this.request.customerPhone.trim()) return this.fail('يرجى كتابة رقم هاتف العميل.');
    if (!this.request.deviceName.trim()) return this.fail('يرجى كتابة اسم أو نوع الجهاز.');
    if ((this.request.expectedCost ?? 0) < 0 || (this.request.deposit ?? 0) < 0) {
      return this.fail('المبالغ لا يمكن أن تكون سالبة.');
    }

    this.submitting = true;
    // receivedBy / store are taken from the signed-in user's token on the server
    this.maintenanceService.createTicket(this.request).subscribe({
      next: (ticket) => {
        this.submitting = false;
        this.lastTicket = ticket;
        this.request = this.emptyRequest();
        this.toast.success(`تم استلام الجهاز (${ticket.deviceName}) برقم تذكرة ${ticket.ticketNumber}`);
      },
      error: (err) => {
        this.submitting = false;
        this.fail(err.error?.message || this.firstValidationError(err) || 'حدث خطأ أثناء حفظ تذكرة الصيانة.');
      }
    });
  }

  private fail(message: string): void {
    this.errorMessage = message;
    this.toast.error(message);
  }

  private firstValidationError(err: any): string | null {
    const errors = err?.error?.errors;
    if (!errors) return null;
    const first = Object.values(errors)[0] as string[] | undefined;
    return first?.[0] ?? null;
  }
}

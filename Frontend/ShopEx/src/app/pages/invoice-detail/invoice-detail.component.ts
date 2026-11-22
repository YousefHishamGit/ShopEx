import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { SupplierInvoice, AddPaymentRequest } from '../../models/models';
import { PrintService } from '../../shared/print/print.service';
import { PrintSheetComponent } from '../../shared/print/print-sheet.component';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, PrintSheetComponent],
  templateUrl: './invoice-detail.component.html'
})
export class InvoiceDetailComponent implements OnInit {
  invoiceId!: number;
  invoice: SupplierInvoice | null = null;
  loading: boolean = true;
  errorMessage: string = '';
  successMessage: string = '';
  autoPrint: boolean = false;
  readonly today = new Date();

  // Payment Modal State
  showPaymentModal: boolean = false;
  paymentAmount: number = 0;
  paymentDate: string = new Date().toISOString().substring(0, 10);
  paymentMethod: string = 'Cash';
  submittingPayment: boolean = false;
  paymentError: string = '';

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router,
    private printer: PrintService
  ) {}

  ngOnInit(): void {
    // Check if auto-print was requested (e.g. after creating an invoice)
    this.route.queryParams.subscribe(q => {
      this.autoPrint = q['print'] === 'true' || q['print'] === true;
    });

    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        this.invoiceId = parseInt(idParam, 10);
        this.fetchInvoice();
      }
    });
  }

  fetchInvoice(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.getInvoiceById(this.invoiceId).subscribe({
      next: (data) => {
        this.invoice = data;
        this.paymentAmount = data.remainingAmount;
        this.loading = false;

        // Automatically trigger print if requested
        if (this.autoPrint) {
          setTimeout(() => {
            this.printInvoice();
          }, 400);
        }
      },
      error: (err) => {
        console.error('Error loading invoice', err);
        this.errorMessage = `فشل في تحميل تفاصيل الفاتورة #${this.invoiceId}.`;
        this.loading = false;
      }
    });
  }

  get isPaid(): boolean {
    return !!this.invoice && this.invoice.remainingAmount <= 0;
  }

  get isPartial(): boolean {
    return !!this.invoice && this.invoice.remainingAmount > 0 && this.invoice.paidAmount > 0;
  }

  get statusLabel(): string {
    return this.isPaid ? 'مدفوعة بالكامل' : (this.isPartial ? 'مدفوعة جزئياً' : 'غير مدفوعة');
  }

  paymentMethodLabel(method?: string | null): string {
    switch ((method || '').toLowerCase()) {
      case 'cash': case 'نقدي': case '': return 'نقداً';
      case 'banktransfer': return 'تحويل بنكي';
      case 'card': return 'بطاقة';
      case 'check': return 'شيك';
      default: return method || 'نقداً';
    }
  }

  printInvoice(): void {
    this.printer.print();
  }

  goBack(): void {
    if (this.invoice && this.invoice.rmsSupplierID) {
      this.router.navigate(['/suppliers', this.invoice.rmsSupplierID]);
    } else {
      this.router.navigate(['/suppliers']);
    }
  }

  openPaymentModal(): void {
    if (this.invoice) {
      this.paymentAmount = this.invoice.remainingAmount;
      this.paymentError = '';
      this.showPaymentModal = true;
    }
  }

  closePaymentModal(): void {
    this.showPaymentModal = false;
  }

  submitPayment(): void {
    if (!this.invoice) return;
    this.paymentError = '';

    if (this.paymentAmount <= 0) {
      this.paymentError = 'يجب أن يكون مبلغ الدفعة أكبر من صفر.';
      return;
    }
    if (this.paymentAmount > this.invoice.remainingAmount) {
      this.paymentError = `المبلغ المسدد لا يمكن أن يتجاوز المتبقي المستحق (${this.invoice.remainingAmount.toFixed(2)} ج.م).`;
      return;
    }

    const payload: AddPaymentRequest = {
      amount: Number(this.paymentAmount),
      paymentDate: this.paymentDate,
      paymentMethod: this.paymentMethod.trim()
    };

    this.submittingPayment = true;
    this.api.addPayment(this.invoiceId, payload).subscribe({
      next: (updated) => {
        this.submittingPayment = false;
        this.invoice = updated;
        this.showPaymentModal = false;
        this.successMessage = `تم تسجيل دفعة بقيمة ${payload.amount.toFixed(2)} ج.م بنجاح!`;
        setTimeout(() => this.successMessage = '', 4000);
      },
      error: (err) => {
        this.submittingPayment = false;
        console.error('Error adding payment', err);
        this.paymentError = err.error?.message || err.message || 'فشل في تسجيل الدفعة.';
      }
    });
  }
}

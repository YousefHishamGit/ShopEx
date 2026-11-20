import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SupplierReturnService } from '../../../services/supplier-return.service';
import { SupplierReturn, AddSupplierRefundRequest, SupplierReturnStatus, SupplierReturnSettlementType } from '../../../models/models';

@Component({
  selector: 'app-return-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './return-detail.component.html'
})
export class ReturnDetailComponent implements OnInit {
  returnId!: number;
  returnDetails: SupplierReturn | null = null;

  loading: boolean = false;
  confirming: boolean = false;
  cancelling: boolean = false;
  refunding: boolean = false;

  errorMessage: string = '';
  successMessage: string = '';

  // Add Refund Modal
  showRefundModal: boolean = false;
  refundRequest: AddSupplierRefundRequest = {
    amount: 0,
    refundDate: new Date().toISOString().substring(0, 10),
    paymentMethod: 'Cash',
    notes: ''
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private returnService: SupplierReturnService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.returnId = parseInt(id, 10);
        this.loadDetails();
      }
    });
  }

  loadDetails(): void {
    this.loading = true;
    this.errorMessage = '';
    this.returnService.getReturnById(this.returnId).subscribe({
      next: (data) => {
        this.returnDetails = data;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'فشل في تحميل تفاصيل المرتجع: ' + (err.error?.message || err.message);
        this.loading = false;
      }
    });
  }

  confirmReturn(): void {
    if (!confirm('هل أنت متأكد من رغبتك في تأكيد المرتجع؟ سيتم خصم الكميات المحددة فوراً من رصيد المخزن المختار.')) {
      return;
    }

    this.confirming = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.returnService.confirmReturn(this.returnId).subscribe({
      next: (updated) => {
        this.confirming = false;
        this.returnDetails = updated;
        this.successMessage = 'تم تأكيد المرتجع وخصم المخزون بنجاح!';
      },
      error: (err) => {
        this.confirming = false;
        alert('فشل تأكيد المرتجع: ' + (err.error?.message || err.message));
      }
    });
  }

  cancelReturn(): void {
    if (!confirm('هل أنت متأكد من إلغاء هذا المرتجع؟')) {
      return;
    }

    this.cancelling = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.returnService.cancelReturn(this.returnId).subscribe({
      next: (updated) => {
        this.cancelling = false;
        this.returnDetails = updated;
        this.successMessage = 'تم إلغاء المرتجع بنجاح.';
      },
      error: (err) => {
        this.cancelling = false;
        alert('فشل إلغاء المرتجع: ' + (err.error?.message || err.message));
      }
    });
  }

  openRefundModal(): void {
    if (!this.returnDetails) return;
    this.refundRequest = {
      amount: this.returnDetails.pendingRefund,
      refundDate: new Date().toISOString().substring(0, 10),
      paymentMethod: 'Cash',
      notes: ''
    };
    this.showRefundModal = true;
  }

  closeRefundModal(): void {
    this.showRefundModal = false;
  }

  submitRefund(): void {
    if (!this.refundRequest.amount || this.refundRequest.amount <= 0) {
      alert('مبلغ الاسترداد يجب أن يكون أكبر من صفر.');
      return;
    }

    this.refunding = true;
    this.returnService.addRefund(this.returnId, this.refundRequest).subscribe({
      next: (updated) => {
        this.refunding = false;
        this.showRefundModal = false;
        this.returnDetails = updated;
        this.successMessage = 'تم تسجيل الاسترداد النقدي بنجاح!';
      },
      error: (err) => {
        this.refunding = false;
        alert('فشل تسجيل الاسترداد: ' + (err.error?.message || err.message));
      }
    });
  }

  get isDraft(): boolean {
    return this.returnDetails?.status === SupplierReturnStatus.Draft;
  }

  get isConfirmed(): boolean {
    return this.returnDetails?.status === SupplierReturnStatus.Confirmed;
  }

  get isCashRefund(): boolean {
    return this.returnDetails?.settlementType === SupplierReturnSettlementType.CashRefund;
  }
}

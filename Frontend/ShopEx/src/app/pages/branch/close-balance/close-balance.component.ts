import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BalanceService } from '../../../services/balance.service';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';
import { BalanceTransactionItem, BalanceType, StoreBalanceGroup } from '../../../models/models';

@Component({
  selector: 'app-close-balance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './close-balance.component.html'
})
export class CloseBalanceComponent implements OnInit {
  groups: StoreBalanceGroup[] = [];
  ledger: BalanceTransactionItem[] = [];

  loading = false;
  errorMessage = '';

  // Close modal
  target: StoreBalanceGroup | null = null;
  amountUsed: number | null = null;
  profit: number | null = null;
  notes = '';
  submitting = false;
  modalError = '';

  // Self-charge modal
  readonly balanceTypes = [
    { value: BalanceType.VodafoneCash, label: 'فودافون كاش' },
    { value: BalanceType.OrangeCash, label: 'أورنج كاش' },
    { value: BalanceType.EtisalatCash, label: 'اتصالات كاش' },
    { value: BalanceType.instapay, label: 'انستاباي' },
    { value: BalanceType.Cards, label: 'فيزا / كروت' },
    { value: BalanceType.Fawry, label: 'فوري' }
  ];
  showCharge = false;
  chargeType: number | null = null;
  chargeAmount: number | null = null;
  chargeNotes = '';
  chargeError = '';

  constructor(
    private balanceService: BalanceService,
    private auth: AuthService,
    private toast: ToastService
  ) { }

  get branchName(): string {
    return this.auth.currentUser()?.storeName || '';
  }

  ngOnInit(): void {
    this.load();
  }

  /** The API only returns the signed-in employee's own branch, so no store filter is sent. */
  load(): void {
    this.loading = true;
    this.errorMessage = '';

    this.balanceService.getGrouped().subscribe({
      next: (data) => {
        this.groups = data || [];
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'تعذر تحميل أرصدة الفرع.';
        this.loading = false;
      }
    });

  }

  openCharge(preselect?: StoreBalanceGroup): void {
    this.chargeType = preselect ? preselect.balanceType : null;
    this.chargeAmount = null;
    this.chargeNotes = '';
    this.chargeError = '';
    this.showCharge = true;
  }

  closeCharge(): void {
    if (this.submitting) return;
    this.showCharge = false;
  }

  submitCharge(): void {
    if (this.submitting) return;

    const amount = Number(this.chargeAmount);
    if (!this.chargeType) {
      this.chargeError = 'يرجى اختيار وسيلة الدفع.';
      return;
    }
    if (!amount || amount <= 0) {
      this.chargeError = 'يرجى إدخال مبلغ الشحن (أكبر من صفر).';
      return;
    }

    this.submitting = true;
    this.chargeError = '';

    this.balanceService
      .chargeOwn({
        balanceType: Number(this.chargeType),
        amount,
        notes: this.chargeNotes.trim() || undefined
      })
      .subscribe({
        next: (r) => {
          this.submitting = false;
          this.showCharge = false;
          this.toast.success(`تم شحن ${amount} ج.م (${r.balanceTypeName}).`);
          this.load();
        },
        error: (err) => {
          this.submitting = false;
          this.chargeError = err.error?.message || 'تعذر تسجيل الشحن.';
        }
      });
  }

  trackGroup = (_: number, g: StoreBalanceGroup) => g.balanceType;
  trackTx = (_: number, t: BalanceTransactionItem) => `${t.transactionType}-${t.id}`;

  openModal(group: StoreBalanceGroup): void {
    this.target = group;
    this.amountUsed = null;
    this.profit = null;
    this.notes = '';
    this.modalError = '';
  }

  closeModal(): void {
    if (this.submitting) return;
    this.target = null;
  }

  get remainingAfter(): number {
    return (this.target?.currentBalance ?? 0) - (Number(this.amountUsed) || 0);
  }

  submit(): void {
    if (!this.target || this.submitting) return;

    const amount = Number(this.amountUsed);
    if (!amount || amount <= 0) {
      this.modalError = 'يرجى إدخال المبلغ المستخدم (أكبر من صفر).';
      return;
    }
    if (amount > this.target.currentBalance) {
      this.modalError = `المبلغ يتجاوز الرصيد المتاح (${this.target.currentBalance}).`;
      return;
    }

    if (this.profit === null || this.profit === undefined || (this.profit as unknown) === '') {
      this.modalError = 'يرجى إدخال الربح (اكتب 0 لو مفيش ربح).';
      return;
    }
    const profit = Number(this.profit);
    if (isNaN(profit) || profit < 0) {
      this.modalError = 'الربح لا يمكن أن يكون سالباً.';
      return;
    }

    const group = this.target;
    this.submitting = true;
    this.modalError = '';

    // storeId is required by the request shape, but the server replaces it with the branch from the token
    this.balanceService
      .closeStore({
        storeId: group.storeId,
        balanceType: group.balanceType,
        amountUsed: amount,
        profit,
        notes: this.notes.trim() || undefined
      })
      .subscribe({
        next: () => {
          this.submitting = false;
          this.target = null;
          this.toast.success(`تم تقفيل ${amount} ج.م (الربح ${profit} ج.م) من ${group.balanceTypeName}.`);
          this.load();
        },
        error: (err) => {
          this.submitting = false;
          this.modalError = err.error?.message || 'تعذر تسجيل التقفيل.';
        }
      });
  }
}

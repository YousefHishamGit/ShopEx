import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ExpenseService } from '../../../services/expense.service';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';
import { ExpensePaymentMethod, ExpenseResponse, ExpenseSummaryDto } from '../../../models/models';

@Component({
  selector: 'app-branch-expenses',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './branch-expenses.component.html'
})
export class BranchExpensesComponent implements OnInit, OnDestroy {
  categories: { id: number; name: string }[] = [];

  expenses: ExpenseResponse[] = [];
  summary: ExpenseSummaryDto | null = null;

  // Paging + date filter (default: today)
  page = 1;
  readonly pageSize = 25;
  fromDate = this.todayStr();
  toDate = this.todayStr();

  loading = false;
  errorMessage = '';
  private loadSub?: Subscription;

  // Add modal
  showModal = false;
  submitting = false;
  modalError = '';
  form = this.emptyForm();

  readonly paymentMethods = [
    { value: ExpensePaymentMethod.Cash, label: 'نقدي' },
    { value: ExpensePaymentMethod.BankTransfer, label: 'تحويل بنكي / انستاباي' },
    { value: ExpensePaymentMethod.CreditCard, label: 'بطاقة' },
    { value: ExpensePaymentMethod.ElectronicWallet, label: 'محفظة إلكترونية' }
  ];

  constructor(
    private expenseService: ExpenseService,
    private auth: AuthService,
    private toast: ToastService
  ) { }

  get branchName(): string {
    return this.auth.currentUser()?.storeName || '';
  }

  ngOnInit(): void {
    this.expenseService.getBranchCategories().subscribe({
      next: (c) => (this.categories = c || []),
      error: () => { }
    });
    this.load();
  }

  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
  }

  load(): void {
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.errorMessage = '';

    this.loadSub = this.expenseService
      .getBranchExpenses(this.page, this.pageSize, this.fromDate || undefined, this.toDate || undefined)
      .subscribe({
        next: (s) => {
          this.summary = s;
          this.expenses = s.items;
          this.page = s.page;
          this.loading = false;
        },
        error: (err) => {
          this.errorMessage = err?.error?.message || 'تعذر تحميل مصروفات الفرع.';
          this.loading = false;
        }
      });
  }

  onDateChange(): void {
    if (this.fromDate && this.toDate && this.fromDate > this.toDate) return;
    this.page = 1;
    this.load();
  }

  setToday(): void {
    this.fromDate = this.toDate = this.todayStr();
    this.onDateChange();
  }

  showAll(): void {
    this.fromDate = this.toDate = '';
    this.onDateChange();
  }

  goToPage(p: number): void {
    const total = this.summary?.totalPages ?? 1;
    if (p < 1 || p > total || p === this.page) return;
    this.page = p;
    this.load();
  }

  get pageFrom(): number {
    return this.summary && this.summary.totalCount > 0 ? (this.page - 1) * this.pageSize + 1 : 0;
  }

  get pageTo(): number {
    return this.summary ? Math.min(this.page * this.pageSize, this.summary.totalCount) : 0;
  }

  // ---------- Add expense ----------
  openModal(): void {
    this.form = this.emptyForm();
    this.modalError = '';
    this.showModal = true;
  }

  closeModal(): void {
    if (this.submitting) return;
    this.showModal = false;
  }

  submit(): void {
    if (this.submitting) return;

    const amount = Number(this.form.amount);
    if (!amount || amount <= 0) {
      this.modalError = 'يرجى إدخال المبلغ (أكبر من صفر).';
      return;
    }
    const newName = this.form.categoryName.trim();
    if (!this.form.categoryId && !newName) {
      this.modalError = 'يرجى اختيار بند المصروف أو كتابة بند جديد.';
      return;
    }
    if (!this.form.notes.trim()) {
      this.modalError = 'يرجى كتابة تفصيل المصروف (صُرف في إيه).';
      return;
    }

    this.submitting = true;
    this.modalError = '';

    this.expenseService
      .createBranchExpense({
        categoryId: this.form.categoryId ?? undefined,
        categoryName: this.form.categoryId ? undefined : newName,
        amount,
        paymentMethod: Number(this.form.paymentMethod),
        recipient: this.form.recipient.trim() || undefined,
        notes: this.form.notes.trim()
      })
      .subscribe({
        next: () => {
          this.submitting = false;
          this.showModal = false;
          this.toast.success(`تم تسجيل مصروف ${amount} ج.م.`);
          this.page = 1;
          this.load();
          // قد يكون أُنشئ بند جديد
          this.expenseService.getBranchCategories().subscribe({ next: (c) => (this.categories = c || []), error: () => { } });
        },
        error: (err) => {
          this.submitting = false;
          this.modalError = err?.error?.message || 'تعذر تسجيل المصروف.';
        }
      });
  }

  trackExpense = (_: number, e: ExpenseResponse) => e.id;

  private emptyForm() {
    return {
      categoryId: null as number | null,
      categoryName: '',
      amount: null as number | null,
      paymentMethod: ExpensePaymentMethod.Cash as number,
      recipient: '',
      notes: ''
    };
  }

  /** yyyy-MM-dd بالتوقيت المحلي */
  private todayStr(): string {
    const d = new Date();
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${d.getFullYear()}-${m}-${day}`;
  }
}
